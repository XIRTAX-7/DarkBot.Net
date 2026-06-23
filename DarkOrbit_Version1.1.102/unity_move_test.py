#!/usr/bin/env python3
"""Проверка исходящего движения: клиент vs сервер (MoveRequest / hero_pos)."""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import frida

AGENT_PATH = Path(__file__).with_name("unity_bridge_agent.js")
PROCESS_NAME = "DarkOrbit.exe"
MAP_CENTER_X = 10_500
MAP_CENTER_Y = 6_550

_events: list[dict] = []


def on_message(message: dict, _data) -> None:
    if message.get("type") == "error":
        print(f"[frida-error] {message.get('description', message)}", flush=True)
        return

    payload = message.get("payload")
    if payload is None:
        return

    if isinstance(payload, str):
        print(payload, flush=True)
        return

    event_type = payload.get("type", "?")
    if event_type == "ping":
        return

    _events.append(payload)
    print(json.dumps(payload, ensure_ascii=False), flush=True)


def parse_json(raw: str) -> dict:
    return json.loads(raw)


def distance_to_center(x: int, y: int) -> float:
    return ((x - MAP_CENTER_X) ** 2 + (y - MAP_CENTER_Y) ** 2) ** 0.5


def count_event(event_type: str, after_ts: float | None = None) -> int:
    total = 0
    for event in _events:
        if event.get("type") != event_type:
            continue
        if after_ts is not None and event.get("ts", 0) < after_ts:
            continue
        total += 1
    return total


def main() -> int:
    parser = argparse.ArgumentParser(description="Unity move server/client verification")
    parser.add_argument("-n", "--seconds", type=int, default=20, help="Observe duration after move")
    parser.add_argument("-p", "--process", default=PROCESS_NAME, help="Process name")
    parser.add_argument(
        "-a",
        "--agent",
        type=Path,
        default=AGENT_PATH,
        help="Path to unity_bridge_agent.js",
    )
    parser.add_argument(
        "--warmup",
        type=float,
        default=2.0,
        help="Seconds before move command",
    )
    parser.add_argument(
        "--manual-only",
        action="store_true",
        help="Only attach hooks; do not call moveToCenter (click map manually)",
    )
    args = parser.parse_args()

    if not args.agent.is_file():
        print(f"Agent not found: {args.agent}", file=sys.stderr)
        return 1

    source = args.agent.read_text(encoding="utf-8")
    print(f"Attaching to {args.process}...", flush=True)

    try:
        session = frida.attach(args.process)
    except frida.ProcessNotFoundError:
        print(f"Process not found: {args.process}", file=sys.stderr)
        return 1

    script = session.create_script(source)
    script.on("message", on_message)
    script.load()

    status = parse_json(script.exports_sync.get_status())
    print(f"Status: {json.dumps(status, ensure_ascii=False)}", flush=True)

    if args.warmup > 0:
        print(f"Warmup {args.warmup}s...", flush=True)
        time.sleep(args.warmup)

    move_ts = time.time() * 1000
    if args.manual_only:
        print("Manual mode: click on the map now.", flush=True)
    else:
        move_result = parse_json(script.exports_sync.move_to_center())
        print(f"moveToCenter: {json.dumps(move_result, ensure_ascii=False)}", flush=True)
        if not move_result.get("ok"):
            script.exports_sync.stop()
            session.detach()
            return 2

        if move_result.get("clientMoveStarted"):
            print("RPC result: client MoveHeroToCoordinates invoked.", flush=True)
        else:
            print("WARNING: client move animation may not have started.", flush=True)

        if move_result.get("serverPacketSent"):
            print("RPC result: outgoing MoveRequest detected immediately.", flush=True)
        else:
            print(
                "RPC result: NO outgoing MoveRequest yet.",
                flush=True,
            )

        if move_result.get("moveRequestYValid") is False:
            print(
                "WARNING: MoveRequest.targetY mismatch — check client/server Y sign.",
                flush=True,
            )
        elif move_result.get("moveRequestYValid"):
            print("RPC result: MoveRequest.targetY matches server map Y.", flush=True)

        last_mr = move_result.get("lastMoveRequest")
        if last_mr:
            print(
                f"MoveRequest: from ({last_mr.get('positionX')}, {last_mr.get('positionY')}) "
                f"-> ({last_mr.get('targetX')}, {last_mr.get('targetY')})",
                flush=True,
            )

    print(f"Observing for {args.seconds}s...", flush=True)
    time.sleep(args.seconds)

    ctor_count = count_event("move_request_ctor", after_ts=move_ts)
    send_count = count_event("session_send_move", after_ts=move_ts)
    hero_pos_count = count_event("hero_pos", after_ts=move_ts)
    unit_move_count = count_event("unit_move", after_ts=move_ts)

    print("--- summary ---", flush=True)
    print(f"move_request_ctor after command: {ctor_count}", flush=True)
    print(f"session_send_move after command: {send_count}", flush=True)
    print(f"hero_pos (server ack) after command: {hero_pos_count}", flush=True)
    print(f"unit_move after command: {unit_move_count}", flush=True)

    if ctor_count > 0 or send_count > 0:
        print("RESULT: client sent MoveRequest to server.", flush=True)
    else:
        print("RESULT: client did NOT send MoveRequest — movement was client-only.", flush=True)

    if hero_pos_count > 0:
        print("RESULT: server hero_pos updates observed.", flush=True)
    else:
        print("RESULT: no server hero_pos updates — ship likely stayed server-side.", flush=True)

    net_stats = parse_json(script.exports_sync.get_net_stats())
    print(f"netStats: {json.dumps(net_stats, ensure_ascii=False)}", flush=True)

    try:
        script.exports_sync.stop()
    except Exception:
        pass

    session.detach()
    print("Done.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
