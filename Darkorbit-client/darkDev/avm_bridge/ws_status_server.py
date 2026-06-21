from __future__ import annotations

import asyncio
import json
import threading
from typing import Any

from avm_bridge.event_bus import BridgeEventBus
from avm_bridge.game_state_store import GameStateStore
from avm_bridge.models import HEARTBEAT_INTERVAL_SEC
from avm_bridge.status_coalescer import StatusCoalescer

try:
    import websockets
    from websockets.asyncio.server import serve
    from websockets.server import WebSocketServerProtocol
except ImportError as exc:
    raise ImportError("pip install websockets>=12.0") from exc


class WebSocketStatusServer:
    """Push status/events to .NET; snapshot-on-connect; ping/pong heartbeat."""

    def __init__(
        self,
        port: int,
        store: GameStateStore,
        bus: BridgeEventBus,
        coalescer: StatusCoalescer,
        heartbeat_sec: float = HEARTBEAT_INTERVAL_SEC,
    ) -> None:
        self._port = port
        self._store = store
        self._bus = bus
        self._coalescer = coalescer
        self._heartbeat_sec = heartbeat_sec
        self._clients: set[WebSocketServerProtocol] = set()
        self._clients_lock = threading.Lock()
        self._loop: asyncio.AbstractEventLoop | None = None
        self._thread: threading.Thread | None = None
        self._stop_event = threading.Event()

        bus.subscribe_status(self._enqueue_broadcast)
        bus.subscribe_event(self._enqueue_event)

    def start(self) -> None:
        self._thread = threading.Thread(target=self._run_loop, daemon=True, name="ws-status")
        self._thread.start()

    def stop(self) -> None:
        self._stop_event.set()
        if self._loop is not None:
            self._loop.call_soon_threadsafe(self._loop.stop)

    def _run_loop(self) -> None:
        self._loop = asyncio.new_event_loop()
        asyncio.set_event_loop(self._loop)
        self._loop.run_until_complete(self._serve())

    async def _serve(self) -> None:
        async with serve(
            self._handle_client,
            "127.0.0.1",
            self._port,
            process_request=self._process_request,
        ):
            heartbeat = asyncio.create_task(self._heartbeat_loop())
            try:
                while not self._stop_event.is_set():
                    await asyncio.sleep(0.5)
            finally:
                heartbeat.cancel()

    @staticmethod
    def _process_request(connection, request):
        if request.path == "/ws" or request.path.startswith("/ws?"):
            return None
        if request.path == "/":
            body = b'{"endpoints":["WS /ws","GET /status","POST /move"]}'
            return connection.respond(200, body, headers=[("Content-Type", "application/json")])
        return connection.respond(404, b"Not Found")

    async def _handle_client(self, websocket: WebSocketServerProtocol) -> None:
        with self._clients_lock:
            self._clients.add(websocket)
        try:
            snapshot = self._store.make_status_message("snapshot")
            await websocket.send(json.dumps(snapshot))

            async for raw in websocket:
                try:
                    msg = json.loads(raw)
                except json.JSONDecodeError:
                    continue
                if msg.get("type") == "pong":
                    pass
        finally:
            with self._clients_lock:
                self._clients.discard(websocket)

    def _enqueue_broadcast(self, wire: dict[str, Any]) -> None:
        if self._loop is None:
            return
        asyncio.run_coroutine_threadsafe(self._broadcast(wire), self._loop)

    def _enqueue_event(self, event) -> None:
        if self._loop is None:
            return
        asyncio.run_coroutine_threadsafe(
            self._broadcast(event.to_wire()), self._loop
        )

    async def _broadcast(self, wire: dict[str, Any]) -> None:
        data = json.dumps(wire)
        with self._clients_lock:
            clients = list(self._clients)
        for client in clients:
            try:
                await client.send(data)
            except Exception:
                with self._clients_lock:
                    self._clients.discard(client)

    async def _heartbeat_loop(self) -> None:
        while not self._stop_event.is_set():
            await asyncio.sleep(self._heartbeat_sec)
            ping = {"type": "ping", "ts": int(asyncio.get_event_loop().time() * 1000)}
            await self._broadcast(ping)
