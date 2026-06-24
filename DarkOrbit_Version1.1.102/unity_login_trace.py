#!/usr/bin/env python3
"""Запись цепочки ручного входа Unity-клиента через Frida."""

from __future__ import annotations

import argparse
import json
import sys
import time
from datetime import datetime
from pathlib import Path

import frida
import psutil

# Ключевые фазы для краткой сводки (порядок примерный, не все обязательны)
KEY_PHASES = {
    "open_web_login",
    "get_post",
    "update_web_data",
    "loading_ui_show",
    "launch_show_pre_init",
    "launch_show_init",
    "launch_show_start",
    "launch_show_hangar_ready",
    "launch_show_update_button",
    "user_click_start_candidate",
    "ui_button_press",
    "start_click_snapshot",
    "user_click_ship",
    "map_load_start_game",
    "enter_game",
}


def find_darkorbit_pid() -> int | None:
    for proc in psutil.process_iter(["pid", "name"]):
        if proc.info["name"] == "DarkOrbit.exe":
            return proc.info["pid"]
    return None


def format_extra(payload: dict) -> str:
    phase = payload.get("phase", "")
    if phase == "get_post":
        return f" url={payload.get('url')}"
    if phase == "update_web_data":
        return f" len={payload.get('webDataLength')}"
    if phase == "open_web_login":
        return f" url={payload.get('url')}"
    if phase in ("user_click_start_candidate",):
        return f" handler={payload.get('handler')}"
    if phase in ("ui_button_press", "ui_button_pointer_click"):
        return f" btn={payload.get('button')}"
    if phase == "start_click_snapshot":
        return (
            f" pressed={payload.get('pressedButton')}"
            f" btnStart@0x44={payload.get('btnStartField')}"
            f" match={payload.get('buttonsMatch')}"
            f" +{payload.get('msSinceLaunchShowStart')}ms"
        )
    if phase == "launch_show_start" and payload.get("btnStartAtStart"):
        return f" btnStartAtStart={payload.get('btnStartAtStart')}"
    if phase == "user_click_ship":
        return " *** SHIP CLICK ***"
    if phase == "map_load_start_game":
        return " *** MAP LOAD ***"
    return ""


def print_summary(events: list[dict]) -> None:
    trace_events = [e for e in events if e.get("type") == "login_trace"]
    if not trace_events:
        print("(no trace events)")
        return

    print("\n--- Полная цепочка ---")
    for e in trace_events:
        phase = e.get("phase", "?")
        delta = e.get("sincePrevMs", 0)
        total = e.get("sinceStartMs", 0)
        print(f"  +{delta:5d}ms  @{total:6d}ms  {phase}{format_extra(e)}")

    print("\n--- Ключевые точки входа ---")
    key_only = [e for e in trace_events if e.get("phase") in KEY_PHASES]
    for e in key_only:
        phase = e.get("phase", "?")
        total = e.get("sinceStartMs", 0)
        print(f"  @{total:6d}ms  {phase}{format_extra(e)}")

    start_candidates = [
        e.get("handler")
        for e in trace_events
        if e.get("phase") == "user_click_start_candidate"
    ]
    if start_candidates:
        print("\n--- Кандидаты на кнопку START (повтори клик — смотри какой сработал) ---")
        for name in dict.fromkeys(start_candidates):
            print(f"  - {name}")

    ship_clicks = [e for e in trace_events if e.get("phase") == "user_click_ship"]
    map_starts = [e for e in trace_events if e.get("phase") == "map_load_start_game"]
    if ship_clicks and map_starts:
        ship_t = ship_clicks[0].get("sinceStartMs", 0)
        map_t = map_starts[0].get("sinceStartMs", 0)
        print(f"\n--- Тайминг: корабль → карта: {map_t - ship_t}ms ---")

    snapshots = [e for e in trace_events if e.get("phase") == "start_click_snapshot"]
    map_starts_all = [e for e in trace_events if e.get("phase") == "map_load_start_game"]
    if snapshots:
        print("\n--- Снимок для unity_bridge_agent (нажатие START) ---")
        for i, snap in enumerate(snapshots, 1):
            map_after = next(
                (m for m in map_starts_all if m.get("sinceStartMs", 0) >= snap.get("sinceStartMs", 0)),
                None,
            )
            led_to_map = map_after is not None and (
                map_after.get("sinceStartMs", 0) - snap.get("sinceStartMs", 0) < 500
            )
            print(f"  [{i}] pressedButton     = {snap.get('pressedButton')}")
            print(f"      launchShow        = {snap.get('launchShow')}")
            print(f"      btnStart @+0x44    = {snap.get('btnStartField')}  (offset 0x{snap.get('btnStartOffset', 44):x})")
            print(f"      buttonsMatch      = {snap.get('buttonsMatch')}")
            print(f"      gameLoadingManager= {snap.get('gameLoadingManager')}")
            print(f"      glm.launchShow    = {snap.get('glmLaunchShowField')}  (offset 0x{snap.get('glmLaunchShowOffset', 0x1c):x})")
            print(f"      ms after Start()  = {snap.get('msSinceLaunchShowStart')}")
            print(f"      led to map load   = {led_to_map}")
            if snap.get("source"):
                print(f"      source            = {snap.get('source')}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Trace Unity manual login + main menu + map load flow",
    )
    parser.add_argument("--pid", type=int, default=0, help="DarkOrbit.exe pid")
    parser.add_argument("--seconds", type=int, default=600, help="Recording duration")
    parser.add_argument(
        "--out",
        type=Path,
        default=None,
        help="Output jsonl path (default: login_trace_<timestamp>.jsonl)",
    )
    args = parser.parse_args()

    pid = args.pid or find_darkorbit_pid()
    if not pid:
        print("DarkOrbit.exe not found — запусти игру вручную", file=sys.stderr)
        return 1

    script_path = Path(__file__).with_name("unity_login_trace.js")
    source = script_path.read_text(encoding="utf-8")
    out_path = args.out or Path(__file__).with_name(
        f"login_trace_{datetime.now().strftime('%Y%m%d_%H%M%S')}.jsonl"
    )

    events: list[dict] = []

    def on_message(message, _data):
        if message.get("type") != "send":
            if message.get("type") == "error":
                print("[frida-error]", message.get("stack", message), file=sys.stderr)
            return
        payload = message.get("payload")
        if not isinstance(payload, dict):
            return
        events.append(payload)
        if payload.get("type") != "login_trace":
            return
        phase = payload.get("phase", "?")
        delta = payload.get("sincePrevMs", 0)
        print(f"[{payload.get('seq', '?'):>3}] +{delta:4d}ms  {phase}{format_extra(payload)}")

    print(f"Attach pid={pid}")
    print(f"Output: {out_path}")
    print()
    print("Сценарий записи:")
    print("  1. Войди в аккаунт (логин/пароль в WebView)")
    print("  2. Дождись главного меню")
    print("  3. Нажми START")
    print("  4. Нажми на корабль")
    print("  5. Дождись загрузки карты")
    print("  Ctrl+C — остановить запись раньше")
    print()

    session = frida.attach(pid)
    script = session.create_script(source)
    script.on("message", on_message)
    script.load()

    deadline = time.time() + args.seconds
    try:
        while time.time() < deadline:
            time.sleep(0.5)
    except KeyboardInterrupt:
        print("\nОстановлено пользователем")

    try:
        script.exports_sync.stop()
    except Exception:
        pass

    session.detach()

    with out_path.open("w", encoding="utf-8") as f:
        for event in events:
            f.write(json.dumps(event, ensure_ascii=False) + "\n")

    print(f"\nSaved {len(events)} events -> {out_path}")
    print_summary(events)

    update_event = next(
        (e for e in events if e.get("phase") == "update_web_data" and (e.get("webDataLength") or 0) > 0),
        None,
    )
    if update_event:
        full = update_event.get("webDataFull") or update_event.get("webDataPreview")
        if full:
            sample_path = out_path.with_suffix(".webdata.json")
            sample_path.write_text(full if isinstance(full, str) else json.dumps(full), encoding="utf-8")
            print(f"\nUpdateWebData sample: {sample_path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
