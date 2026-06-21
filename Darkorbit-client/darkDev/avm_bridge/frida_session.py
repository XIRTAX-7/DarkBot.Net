from __future__ import annotations

import json
import threading
import time
import uuid
from pathlib import Path
from typing import TYPE_CHECKING, Any

import frida

from avm_bridge.heap_loader import HeapRangeLoader
from avm_bridge.models import ActionResult
from avm_bridge.process_finder import ProcessFinder

if TYPE_CHECKING:
    from avm_bridge.game_state_store import GameStateStore


class FridaGameSession:
    """Frida attach, agent RPC, and agent message forwarding — no HTTP/WS knowledge."""

    def __init__(
        self,
        pid: int,
        agent_script_path: Path,
        store: GameStateStore,
    ) -> None:
        self.pid = pid
        self._agent_script_path = agent_script_path
        self._store = store
        self._heap_loader = HeapRangeLoader(agent_script_path)
        self._session: frida.core.Session | None = None
        self._script: frida.core.Script | None = None
        self._ready_event = threading.Event()

    @property
    def is_attached(self) -> bool:
        return self._script is not None

    def attach(self) -> None:
        if not self._agent_script_path.is_file():
            raise FileNotFoundError(f"Frida agent not found: {self._agent_script_path}")

        try:
            self._session = frida.attach(self.pid)
        except frida.ProcessNotFoundError as exc:
            hint = ProcessFinder.find_flash_process()
            msg = f"Process pid {self.pid} not found (client restarted?)."
            if hint:
                msg += f" Current Pepper Flash pid is {hint} — omit -p or use: -p {hint}"
            else:
                msg += " Open the game map first, then run without -p for auto-detect."
            raise RuntimeError(msg) from exc
        except frida.ProcessNotRespondingError as exc:
            raise RuntimeError(
                f"Cannot attach to pid {self.pid}: {exc}\n"
                "Try: DarkOrbit Client with Settings.NoSandbox=true or --no-sandbox"
            ) from exc

        source = self._heap_loader.load_agent_source(self._session, self.pid)
        self._script = self._session.create_script(source)
        self._script.on("message", self._on_message)
        self._script.load()

    def _on_message(self, message: dict, _data) -> None:
        if message.get("type") == "send":
            payload = message.get("payload") or {}
            print(f"[agent] {json.dumps(payload, ensure_ascii=False)}")
            self._store.apply_agent_payload(payload)
            if payload.get("type") in ("ready", "status"):
                state = payload.get("state") or {}
                if state.get("ready"):
                    self._ready_event.set()
        elif message.get("type") == "error":
            print("[agent error]", message.get("stack", message))

    def wait_ready(self, timeout_sec: float = 120.0) -> bool:
        if self._script is None:
            raise RuntimeError("Not attached")

        deadline = time.time() + timeout_sec
        while time.time() < deadline:
            if self._script.exports_sync.is_ready():
                self._sync_status_from_rpc()
                return True
            time.sleep(0.25)
        return False

    def _sync_status_from_rpc(self) -> None:
        if self._script is None:
            return
        raw = self._script.exports_sync.get_status()
        self._store.refresh_from_rpc(json.loads(raw))

    def sync_status_from_rpc(self) -> dict[str, Any]:
        if self._script is None:
            return {"ready": False, "error": "not attached"}
        raw = self._script.exports_sync.get_status()
        state = json.loads(raw)
        self._store.refresh_from_rpc(state)
        return self._store.snapshot()

    def _rpc_json(self, fn_name: str, *args) -> dict[str, Any]:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        fn = getattr(self._script.exports_sync, fn_name)
        raw = fn(*args)
        return json.loads(raw)

    @staticmethod
    def _accepted(result: dict[str, Any]) -> ActionResult:
        command_id = str(uuid.uuid4())
        if result.get("ok"):
            return ActionResult(
                ok=True,
                accepted=True,
                command_id=command_id,
                data={k: v for k, v in result.items() if k not in ("ok", "error")},
            )
        return ActionResult(
            ok=False,
            accepted=False,
            command_id=command_id,
            error=str(result.get("error") or "command failed"),
            data={k: v for k, v in result.items() if k not in ("ok", "error")},
        )

    def move(self, x: float, y: float) -> ActionResult:
        return self._accepted(self._rpc_json("move_to", float(x), float(y)))

    def collect(self, x: float, y: float, collectable_adr: str) -> ActionResult:
        return self._accepted(
            self._rpc_json("collect_to", float(x), float(y), collectable_adr)
        )

    def select(self, args: list[int]) -> ActionResult:
        return self._accepted(self._rpc_json("select_entity", json.dumps(args)))

    def use_item(self, item_id: str, method_index: int, args: list) -> ActionResult:
        return self._accepted(
            self._rpc_json("use_item", item_id, method_index, json.dumps(args))
        )

    def refine(
        self,
        refine_util_address: str,
        ore_id: int,
        amount: int,
        method_index: int | None = None,
    ) -> ActionResult:
        idx = -1 if method_index is None else int(method_index)
        return self._accepted(
            self._rpc_json("refine", refine_util_address, int(ore_id), int(amount), idx)
        )

    def invoke(self, object_ptr: str, method_index: int, args: list) -> ActionResult:
        return self._accepted(
            self._rpc_json(
                "invoke_method", object_ptr, int(method_index), json.dumps(args)
            )
        )

    def list_methods(self, limit: int = 24, object_ptr: str | None = None) -> dict | list:
        if self._script is None:
            return []
        if object_ptr:
            raw = self._script.exports_sync.list_methods(object_ptr, limit)
        else:
            raw = self._script.exports_sync.list_methods(limit)
        return json.loads(raw)

    def detach(self) -> None:
        if self._session is not None:
            try:
                self._session.detach()
            except frida.InvalidOperationError:
                pass
        self._session = None
        self._script = None

    @property
    def ready_event(self) -> threading.Event:
        return self._ready_event
