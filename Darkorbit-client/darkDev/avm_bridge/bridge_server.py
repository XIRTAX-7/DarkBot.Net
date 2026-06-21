from __future__ import annotations

import asyncio
import json
import threading
from typing import TYPE_CHECKING, Any

from aiohttp import web

from avm_bridge.event_bus import BridgeEventBus
from avm_bridge.game_state_store import GameStateStore
from avm_bridge.models import HEARTBEAT_INTERVAL_SEC
from avm_bridge.status_coalescer import StatusCoalescer

if TYPE_CHECKING:
    from avm_bridge.action_dispatcher import ActionDispatcher


class BridgeServer:
    """Unified HTTP + WebSocket on one port (:44570)."""

    def __init__(
        self,
        port: int,
        store: GameStateStore,
        bus: BridgeEventBus,
        coalescer: StatusCoalescer,
        dispatcher: ActionDispatcher,
        heartbeat_sec: float = HEARTBEAT_INTERVAL_SEC,
    ) -> None:
        self._port = port
        self._store = store
        self._bus = bus
        self._coalescer = coalescer
        self._dispatcher = dispatcher
        self._heartbeat_sec = heartbeat_sec
        self._ws_clients: set[web.WebSocketResponse] = set()
        self._loop: asyncio.AbstractEventLoop | None = None
        self._thread: threading.Thread | None = None
        self._runner: web.AppRunner | None = None
        self._stop_event = threading.Event()

        bus.subscribe_status(self._enqueue_broadcast)
        bus.subscribe_event(self._enqueue_event)

    def start(self) -> None:
        self._thread = threading.Thread(target=self._run_loop, daemon=True, name="bridge-server")
        self._thread.start()

    def stop(self) -> None:
        self._stop_event.set()
        if self._loop is not None and self._runner is not None:
            asyncio.run_coroutine_threadsafe(self._shutdown(), self._loop)

    async def _shutdown(self) -> None:
        if self._runner is not None:
            await self._runner.cleanup()

    def _run_loop(self) -> None:
        self._loop = asyncio.new_event_loop()
        asyncio.set_event_loop(self._loop)
        self._loop.run_until_complete(self._serve())

    async def _serve(self) -> None:
        app = web.Application()
        app.router.add_get("/", self._handle_index)
        app.router.add_get("/status", self._handle_status)
        app.router.add_get("/methods", self._handle_methods)
        app.router.add_post("/move", self._handle_post("move"))
        app.router.add_post("/collect", self._handle_post("collect"))
        app.router.add_post("/select", self._handle_post("select"))
        app.router.add_post("/useItem", self._handle_post("useItem"))
        app.router.add_post("/refine", self._handle_post("refine"))
        app.router.add_post("/invoke", self._handle_post("invoke"))
        app.router.add_post("/callMethod", self._handle_post("callMethod"))
        app.router.add_get("/ws", self._handle_ws)

        self._runner = web.AppRunner(app)
        await self._runner.setup()
        site = web.TCPSite(self._runner, "127.0.0.1", self._port)
        await site.start()
        print(f"[+] Bridge server http://127.0.0.1:{self._port}/status ws://127.0.0.1:{self._port}/ws")

        heartbeat = asyncio.create_task(self._heartbeat_loop())
        try:
            while not self._stop_event.is_set():
                await asyncio.sleep(0.5)
        finally:
            heartbeat.cancel()
            await self._runner.cleanup()

    async def _handle_index(self, _request: web.Request) -> web.Response:
        return web.json_response({
            "endpoints": [
                "WS /ws",
                "GET /status",
                "GET /methods[?object=0x...]",
                "POST /move",
                "POST /collect",
                "POST /select",
                "POST /useItem",
                "POST /refine",
                "POST /invoke",
                "POST /callMethod",
            ]
        })

    async def _handle_status(self, _request: web.Request) -> web.Response:
        return web.json_response(self._store.snapshot())

    async def _handle_methods(self, request: web.Request) -> web.Response:
        object_ptr = request.query.get("object")
        return web.json_response({"methods": self._dispatcher.list_methods(object_ptr=object_ptr)})

    def _handle_post(self, action: str):
        async def handler(request: web.Request) -> web.Response:
            try:
                payload = await request.json()
            except json.JSONDecodeError as exc:
                return web.json_response({"ok": False, "error": str(exc)}, status=400)
            try:
                result = self._dispatcher.dispatch(action, payload)
            except (KeyError, ValueError, TypeError) as exc:
                return web.json_response({"ok": False, "error": str(exc)}, status=400)
            code = 202 if result.accepted else (500 if not result.ok else 200)
            return web.json_response(result.to_dict(), status=code)

        return handler

    async def _handle_ws(self, request: web.Request) -> web.WebSocketResponse:
        ws = web.WebSocketResponse()
        await ws.prepare(request)
        self._ws_clients.add(ws)
        try:
            snapshot = self._store.make_status_message("snapshot")
            await ws.send_str(json.dumps(snapshot))
            async for msg in ws:
                if msg.type == web.WSMsgType.TEXT:
                    try:
                        data = json.loads(msg.data)
                    except json.JSONDecodeError:
                        continue
                    if data.get("type") == "pong":
                        pass
                elif msg.type in (web.WSMsgType.CLOSE, web.WSMsgType.ERROR):
                    break
        finally:
            self._ws_clients.discard(ws)
        return ws

    def _enqueue_broadcast(self, wire: dict[str, Any]) -> None:
        if self._loop is None:
            return
        asyncio.run_coroutine_threadsafe(self._broadcast(wire), self._loop)

    def _enqueue_event(self, event) -> None:
        if self._loop is None:
            return
        asyncio.run_coroutine_threadsafe(self._broadcast(event.to_wire()), self._loop)

    async def _broadcast(self, wire: dict[str, Any]) -> None:
        data = json.dumps(wire)
        dead: list[web.WebSocketResponse] = []
        for client in list(self._ws_clients):
            try:
                await client.send_str(data)
            except Exception:
                dead.append(client)
        for client in dead:
            self._ws_clients.discard(client)

    async def _heartbeat_loop(self) -> None:
        while not self._stop_event.is_set():
            await asyncio.sleep(self._heartbeat_sec)
            ping = {"type": "ping", "ts": int(asyncio.get_event_loop().time() * 1000)}
            await self._broadcast(ping)
