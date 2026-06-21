from __future__ import annotations

from collections.abc import Callable
from typing import Any

from avm_bridge.models import BridgeEvent


class BridgeEventBus:
    """Simple pub/sub for bridge-wide events (status push, named events)."""

    def __init__(self) -> None:
        self._status_subscribers: list[Callable[[dict[str, Any]], None]] = []
        self._event_subscribers: list[Callable[[BridgeEvent], None]] = []

    def subscribe_status(self, handler: Callable[[dict[str, Any]], None]) -> None:
        self._status_subscribers.append(handler)

    def subscribe_event(self, handler: Callable[[BridgeEvent], None]) -> None:
        self._event_subscribers.append(handler)

    def publish_status_wire(self, wire: dict[str, Any]) -> None:
        for handler in self._status_subscribers:
            try:
                handler(wire)
            except Exception as exc:
                print(f"[!] BridgeEventBus status handler error: {exc}")

    def publish_event(self, event: BridgeEvent) -> None:
        for handler in self._event_subscribers:
            try:
                handler(event)
            except Exception as exc:
                print(f"[!] BridgeEventBus event handler error: {exc}")
