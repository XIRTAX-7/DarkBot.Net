from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


DEFAULT_PORT = 44570
HEARTBEAT_INTERVAL_SEC = 15.0
STALE_THRESHOLD_SEC = 30.0
STATUS_THROTTLE_MS = 100


@dataclass(frozen=True)
class ActionResult:
    ok: bool
    accepted: bool = False
    command_id: str | None = None
    error: str | None = None
    data: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        body: dict[str, Any] = {"ok": self.ok, "accepted": self.accepted}
        if self.command_id:
            body["commandId"] = self.command_id
        if self.error:
            body["error"] = self.error
        body.update(self.data)
        return body


@dataclass(frozen=True)
class StatusMessage:
    kind: str  # snapshot | update
    seq: int
    ts: float
    data: dict[str, Any]

    def to_wire(self) -> dict[str, Any]:
        return {
            "type": "status",
            "kind": self.kind,
            "seq": self.seq,
            "ts": int(self.ts * 1000),
            "data": self.data,
        }


@dataclass(frozen=True)
class BridgeEvent:
    name: str
    data: dict[str, Any] = field(default_factory=dict)
    message: str | None = None

    def to_wire(self) -> dict[str, Any]:
        wire: dict[str, Any] = {"type": "event", "name": self.name}
        if self.message:
            wire["message"] = self.message
        if self.data:
            wire["data"] = self.data
        return wire
