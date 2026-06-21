from __future__ import annotations

import threading
import time
from typing import TYPE_CHECKING

from avm_bridge.models import STATUS_THROTTLE_MS, BridgeEvent

if TYPE_CHECKING:
    from avm_bridge.event_bus import BridgeEventBus
    from avm_bridge.game_state_store import GameStateStore


class StatusCoalescer:
    """Latest-wins status broadcast with throttle — avoids backpressure on Frida thread."""

    def __init__(
        self,
        store: GameStateStore,
        bus: BridgeEventBus,
        throttle_ms: float = STATUS_THROTTLE_MS,
    ) -> None:
        self._store = store
        self._bus = bus
        self._throttle_sec = throttle_ms / 1000.0
        self._lock = threading.Lock()
        self._pending_kind = "update"
        self._last_sent = 0.0
        self._timer: threading.Timer | None = None

        store.subscribe(self._on_state_changed)

    def _on_state_changed(self, _snapshot: dict, _seq: int, reason: str) -> None:
        kind = "snapshot" if reason in ("ready", "rpc", "snapshot") else "update"
        with self._lock:
            self._pending_kind = kind
            now = time.monotonic()
            elapsed = now - self._last_sent
            if elapsed >= self._throttle_sec:
                self._last_sent = now
                self._flush_locked(kind)
            elif self._timer is None:
                delay = self._throttle_sec - elapsed
                self._timer = threading.Timer(delay, self._timer_flush)
                self._timer.daemon = True
                self._timer.start()

    def _timer_flush(self) -> None:
        with self._lock:
            self._timer = None
            self._last_sent = time.monotonic()
            kind = self._pending_kind
        self._flush_locked(kind)

    def _flush_locked(self, kind: str) -> None:
        wire = self._store.make_status_message(kind)
        self._bus.publish_status_wire(wire)

        if kind == "snapshot" and wire["data"].get("ready"):
            event = BridgeEvent(name="ready", data=wire["data"])
            self._bus.publish_event(event)
        elif wire["data"].get("error"):
            event = BridgeEvent(
                name="error",
                message=str(wire["data"]["error"]),
                data=wire["data"],
            )
            self._bus.publish_event(event)

    def push_snapshot_now(self) -> None:
        """Immediate full snapshot (WS connect, manual refresh)."""
        with self._lock:
            self._last_sent = time.monotonic()
        wire = self._store.make_status_message("snapshot")
        self._bus.publish_status_wire(wire)
