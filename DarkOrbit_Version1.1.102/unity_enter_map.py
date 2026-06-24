#!/usr/bin/env python3
"""Быстрый тест входа на карту — игра уже открыта, ангар виден."""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import frida
import psutil


def find_darkorbit_pid() -> int | None:
    for proc in psutil.process_iter(["pid", "name"]):
        if proc.info["name"] == "DarkOrbit.exe":
            return proc.info["pid"]
    return None


def format_event(payload: dict) -> str:
    phase = payload.get("phase", "?")
    extra = ""
    if phase == "state":
        extra = (
            f" start={payload.get('btnStart')} ship={payload.get('btnShip')}"
        )
    elif phase in ("scheduled_main_thread", "invoke", "invoke_error"):
        extra = f" mode={payload.get('mode')} reason={payload.get('reason')}"
    elif phase == "skip":
        extra = f" reason={payload.get('reason')}"
    elif phase == "map_start":
        extra = " *** MAP LOAD ***"
    elif phase == "enter_game":
        extra = " *** ENTER GAME ***"
    return f"[{payload.get('attempt', '?')}] {phase}{extra}"


def main() -> int:
    parser = argparse.ArgumentParser(description="Try enter map on running DarkOrbit.exe")
    parser.add_argument("--pid", type=int, default=0)
    parser.add_argument("--seconds", type=int, default=30)
    args = parser.parse_args()

    pid = args.pid or find_darkorbit_pid()
    if not pid:
        print("DarkOrbit.exe не найден", file=sys.stderr)
        return 1

    script_path = Path(__file__).with_name("unity_enter_map.js")
    source = script_path.read_text(encoding="utf-8")
    enter_game_seen = False

    def on_message(message, _data):
        nonlocal enter_game_seen
        if message.get("type") != "send":
            if message.get("type") == "error":
                print("[frida-error]", message.get("stack", message), file=sys.stderr)
            return
        payload = message.get("payload")
        if not isinstance(payload, dict):
            return
        if payload.get("type") != "enter_map":
            return
        print(format_event(payload))
        if payload.get("phase") == "enter_game":
            enter_game_seen = True

    print(f"Attach pid={pid}")
    print("Ожидаю ангар — скрипт сам нажмёт корабль/START")
    print()

    session = frida.attach(pid)
    script = session.create_script(source)
    script.on("message", on_message)
    script.load()

    deadline = time.time() + args.seconds
    try:
        while time.time() < deadline and not enter_game_seen:
            time.sleep(0.3)
    except KeyboardInterrupt:
        print("\nОстановлено")

    try:
        state = json.loads(script.exports_sync.get_state())
        print("\n--- state ---")
        print(json.dumps(state, indent=2, ensure_ascii=False))
    except Exception as ex:
        print(f"getState failed: {ex}")

    try:
        script.exports_sync.stop()
    except Exception:
        pass
    session.detach()

    if enter_game_seen:
        print("\nУСПЕХ: enter_game")
        return 0
    print("\nКарта не вошла в EnterGame за отведённое время")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
