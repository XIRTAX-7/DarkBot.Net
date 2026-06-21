from __future__ import annotations

import threading
import time
from pathlib import Path

from avm_bridge.action_dispatcher import ActionDispatcher
from avm_bridge.event_bus import BridgeEventBus
from avm_bridge.frida_session import FridaGameSession
from avm_bridge.game_state_store import GameStateStore
from avm_bridge.bridge_server import BridgeServer
from avm_bridge.models import DEFAULT_PORT
from avm_bridge.process_finder import ProcessFinder
from avm_bridge.status_coalescer import StatusCoalescer


class BridgeService:
    """Composition root — Frida session + hybrid WS status + HTTP commands."""

    def __init__(
        self,
        pid: int,
        port: int = DEFAULT_PORT,
        agent_script_path: Path | None = None,
    ) -> None:
        self.pid = pid
        self.port = port
        base = Path(__file__).resolve().parent.parent
        self._agent_path = agent_script_path or (base / "game_bridge_agent.js")

        self._store = GameStateStore()
        self._bus = BridgeEventBus()
        self._coalescer = StatusCoalescer(self._store, self._bus)
        self._session = FridaGameSession(pid, self._agent_path, self._store)
        self._dispatcher = ActionDispatcher(self._session)
        self._server = BridgeServer(
            port, self._store, self._bus, self._coalescer, self._dispatcher
        )

    @property
    def session(self) -> FridaGameSession:
        return self._session

    @property
    def store(self) -> GameStateStore:
        return self._store

    @property
    def dispatcher(self) -> ActionDispatcher:
        return self._dispatcher

    def attach(self) -> None:
        self._session.attach()

    def start_servers(self) -> None:
        self._server.start()

    def stop_servers(self) -> None:
        self._server.stop()

    def detach(self) -> None:
        self._session.detach()

    def run_serve(self, wait_sec: float = 120.0) -> int:
        print(f"[+] Attaching to pid {self.pid}...")
        self.attach()
        self.start_servers()
        print(f"[+] Waiting for map (up to {wait_sec:.0f}s) — status via WS /ws")

        if not self._session.ready_event.wait(timeout=wait_sec):
            if not self._session.wait_ready(0):
                print("[!] Map not detected yet — stay on the map; watch WS /ws")

        if self._store.snapshot().get("ready"):
            import json
            print("[+] Game ready:", json.dumps(self._store.snapshot(), indent=2))
        else:
            print("[!] Not ready yet — stay on the map")

        try:
            while True:
                time.sleep(1.0)
        except KeyboardInterrupt:
            print("\n[+] Stopping")
        finally:
            self.stop_servers()
            self.detach()
        return 0

    @staticmethod
    def run_move(pid: int, x: float, y: float, wait_sec: float, agent_path: Path | None = None) -> int:
        svc = BridgeService(pid, agent_script_path=agent_path)
        print(f"[+] Attaching to pid {pid}...")
        svc.attach()
        print(f"[+] Waiting for map load (up to {wait_sec:.0f}s)...")
        if not svc.session.wait_ready(wait_sec):
            import json
            print("[!] Timeout — open the game map and retry.")
            print("    Status:", json.dumps(svc.store.snapshot(), indent=2))
            svc.detach()
            return 2
        import json
        print("[+] Ready:", json.dumps(svc.store.snapshot(), indent=2))
        result = svc.dispatcher.dispatch("move", {"x": x, "y": y})
        print("[+] Move result:", json.dumps(result.to_dict(), indent=2))
        svc.detach()
        return 0 if result.ok else 1

    @staticmethod
    def run_status(pid: int, wait_sec: float, agent_path: Path | None = None) -> int:
        import json
        svc = BridgeService(pid, agent_script_path=agent_path)
        svc.attach()
        svc.session.wait_ready(wait_sec)
        svc.session.sync_status_from_rpc()
        print(json.dumps(svc.store.snapshot(), indent=2))
        methods = svc.dispatcher.list_methods()
        if methods:
            for _label, key in [("candidates", "candidates"), ("methods", "methods")]:
                items = methods.get(key) if isinstance(methods, dict) else None
                if items:
                    print(f"\n[eventManager {key}]")
                    for m in items:
                        print(
                            f"  [{m['index']:2d}] {m['name']} "
                            f"params={m['params']} compiled={m['compiled']}"
                        )
        svc.detach()
        return 0

    @staticmethod
    def resolve_pid(explicit_pid: int | None) -> int:
        if explicit_pid:
            return explicit_pid
        return ProcessFinder.find_flash_process()
