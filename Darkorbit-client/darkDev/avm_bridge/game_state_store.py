from __future__ import annotations

import threading
import time
from copy import deepcopy
from typing import Any, Callable


class GameStateStore:
    """Thread-safe game state with monotonic seq — single source for all snapshots."""

    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._state: dict[str, Any] = {"ready": False}
        self._seq = 0
        self._listeners: list[Callable[[dict[str, Any], int, str], None]] = []

    def subscribe(self, listener: Callable[[dict[str, Any], int, str], None]) -> None:
        self._listeners.append(listener)

    def apply_agent_payload(self, payload: dict[str, Any]) -> None:
        msg_type = payload.get("type")
        reason = str(payload.get("reason") or msg_type or "agent")

        if msg_type == "status":
            state = payload.get("state") or {}
            self._merge_state(state, reason)
        elif msg_type == "ready":
            state = payload.get("state") or {}
            self._merge_state(state, "ready")
        elif msg_type == "error":
            error = payload.get("error") or payload.get("message") or "unknown error"
            self._merge_state({"ready": False, "error": str(error)}, "error")
        # warn / method_compiled — logged by session, no store update

    def refresh_from_rpc(self, rpc_state: dict[str, Any]) -> None:
        self._merge_state(rpc_state, "rpc")

    def _merge_state(self, patch: dict[str, Any], reason: str) -> None:
        with self._lock:
            self._state.update(patch)
            self._seq += 1
            seq = self._seq
            snapshot = deepcopy(self._state)

        for listener in self._listeners:
            try:
                listener(snapshot, seq, reason)
            except Exception as exc:
                print(f"[!] GameStateStore listener error: {exc}")

    def snapshot(self) -> dict[str, Any]:
        with self._lock:
            return deepcopy(self._state)

    @property
    def seq(self) -> int:
        with self._lock:
            return self._seq

    def make_status_message(self, kind: str) -> dict[str, Any]:
        with self._lock:
            return {
                "type": "status",
                "kind": kind,
                "seq": self._seq,
                "ts": int(time.time() * 1000),
                "data": deepcopy(self._state),
            }
