#!/usr/bin/env python3
"""Attach unity_bridge_agent.js to DarkOrbit.exe and print events."""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import frida

AGENT_PATH = Path(__file__).with_name("unity_bridge_agent.js")
PROCESS_NAME = "DarkOrbit.exe"


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
        print(
            f"[ping] uptime={payload.get('uptimeMs')}ms agent={payload.get('agentVersion')}",
            flush=True,
        )
        return

    print(json.dumps(payload, ensure_ascii=False), flush=True)


def main() -> int:
    parser = argparse.ArgumentParser(description="Unity bridge agent smoke test")
    parser.add_argument("-n", "--seconds", type=int, default=60, help="Run duration")
    parser.add_argument("-p", "--process", default=PROCESS_NAME, help="Process name")
    parser.add_argument(
        "-a",
        "--agent",
        type=Path,
        default=AGENT_PATH,
        help="Path to unity_bridge_agent.js",
    )
    args = parser.parse_args()

    if not args.agent.is_file():
        print(f"Agent not found: {args.agent}", file=sys.stderr)
        return 1

    source = args.agent.read_text(encoding="utf-8")
    print(f"Attaching to {args.process} for {args.seconds}s...", flush=True)

    try:
        session = frida.attach(args.process)
    except frida.ProcessNotFoundError:
        print(f"Process not found: {args.process}", file=sys.stderr)
        return 1

    script = session.create_script(source)
    script.on("message", on_message)
    script.load()

    try:
        status = script.exports_sync.get_status()
        print(f"RPC status: {status}", flush=True)
    except Exception as exc:
        print(f"RPC get_status failed: {exc}", flush=True)

    print("Move on the map. Waiting for hero_pos / unit_move / map_click...", flush=True)
    time.sleep(args.seconds)

    try:
        script.exports_sync.stop()
    except Exception:
        pass

    session.detach()
    print("Done.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
