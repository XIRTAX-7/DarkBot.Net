from __future__ import annotations

from typing import Any

from avm_bridge.frida_session import FridaGameSession
from avm_bridge.models import ActionResult


class ActionDispatcher:
    """Single entry point for all game commands — HTTP today, WS commands in v2."""

    def __init__(self, session: FridaGameSession) -> None:
        self._session = session

    def dispatch(self, action: str, payload: dict[str, Any]) -> ActionResult:
        handlers = {
            "move": self._move,
            "collect": self._collect,
            "select": self._select,
            "useItem": self._use_item,
            "refine": self._refine,
            "invoke": self._invoke,
            "callMethod": self._invoke,
        }
        handler = handlers.get(action)
        if handler is None:
            return ActionResult(ok=False, accepted=False, error=f"unknown action: {action}")
        return handler(payload)

    def _move(self, payload: dict[str, Any]) -> ActionResult:
        x = float(payload["x"])
        y = float(payload["y"])
        return self._session.move(x, y)

    def _collect(self, payload: dict[str, Any]) -> ActionResult:
        return self._session.collect(
            float(payload["x"]),
            float(payload["y"]),
            str(payload["collectableAdr"]),
        )

    def _select(self, payload: dict[str, Any]) -> ActionResult:
        args = payload["args"]
        if not isinstance(args, list):
            raise ValueError("args must be an array")
        return self._session.select([int(a) for a in args])

    def _use_item(self, payload: dict[str, Any]) -> ActionResult:
        return self._session.use_item(
            str(payload["itemId"]),
            int(payload.get("methodIndex", 19)),
            payload.get("args", []),
        )

    def _refine(self, payload: dict[str, Any]) -> ActionResult:
        return self._session.refine(
            str(payload["refineUtilAddress"]),
            int(payload["oreId"]),
            int(payload["amount"]),
            payload.get("methodIndex"),
        )

    def _invoke(self, payload: dict[str, Any]) -> ActionResult:
        return self._session.invoke(
            str(payload["objectPtr"]),
            int(payload["methodIndex"]),
            payload.get("args", []),
        )

    def list_methods(self, object_ptr: str | None = None) -> dict | list:
        return self._session.list_methods(object_ptr=object_ptr)
