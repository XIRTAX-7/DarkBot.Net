#!/usr/bin/env python3
"""Сравнение ручных кликов: главная карта vs миникарта (цепочка move-событий)."""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import frida

AGENT_PATH = Path(__file__).with_name("unity_bridge_agent.js")
PROCESS_NAME = "DarkOrbit.exe"

MOVE_CHAIN_TYPES = (
    "move_hero_to_coordinates",
    "queue_move_request",
    "queue_move_request_send",
    "move_request_ctor",
    "session_send_move",
)

CLICK_SOURCE_LABELS = {
    "main_map_mouse_down": "карта: mouse down",
    "main_map_hero_move": "карта: HeroMove",
    "main_map_continuous": "карта: drag (continuous)",
    "minimap_click_down": "миникарта: down",
    "minimap_click_up": "миникарта: up",
    "minimap_click": "миникарта: click",
}

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

    if payload.get("type") == "ping":
        return

    _events.append(payload)
    print(json.dumps(payload, ensure_ascii=False), flush=True)


def parse_json(raw: str) -> dict:
    return json.loads(raw)


def is_click_input(event: dict) -> bool:
    return event.get("type") == "click_input"


def is_chain_event(event: dict) -> bool:
    return event.get("type") in MOVE_CHAIN_TYPES


def summarize_click_session(click: dict, window_ms: int = 1500) -> dict:
    t0 = click.get("ts", 0)
    t1 = t0 + window_ms
    window = [e for e in _events if t0 <= e.get("ts", 0) <= t1 and e is not click]

    chain = [e for e in window if is_chain_event(e)]
    chain_types = [e.get("type") for e in chain]

    move_hero = next((e for e in chain if e.get("type") == "move_hero_to_coordinates"), None)
    queue_move = next((e for e in chain if e.get("type") == "queue_move_request"), None)
    move_ctor = next((e for e in chain if e.get("type") == "move_request_ctor"), None)
    session_send = next((e for e in chain if e.get("type") == "session_send_move"), None)

    source = click.get("source", "?")
    label = CLICK_SOURCE_LABELS.get(source, source)

    return {
        "source": source,
        "label": label,
        "heroPos": click.get("heroPos"),
        "chain": chain_types,
        "clientVisual": move_hero is not None,
        "clientQueue": queue_move is not None,
        "serverPacket": move_ctor is not None or session_send is not None,
        "moveHero": move_hero,
        "queueMove": queue_move,
        "moveRequest": move_ctor or session_send,
    }


def print_session_summary(index: int, summary: dict) -> None:
    print(f"\n--- клик #{index}: {summary['label']} ---", flush=True)
    if summary.get("heroPos"):
        pos = summary["heroPos"]
        print(f"  heroPos: ({pos.get('x')}, {pos.get('y')})", flush=True)

    chain = summary.get("chain") or []
    if chain:
        print(f"  цепочка: {' -> '.join(chain)}", flush=True)
    else:
        print("  цепочка: (нет move-событий в окне 1.5с)", flush=True)

    flags = []
    if summary["clientVisual"]:
        flags.append("клиент MoveHeroToCoordinates")
    if summary["clientQueue"]:
        flags.append("QueueMoveRequest")
    if summary["serverPacket"]:
        flags.append("MoveRequest → сервер")
    print(f"  итог: {', '.join(flags) if flags else 'ничего не сработало'}", flush=True)

    move_hero = summary.get("moveHero")
    if move_hero:
        print(
            f"  MoveHero: target=({move_hero.get('targetX')}, {move_hero.get('targetY')})",
            flush=True,
        )

    queue_move = summary.get("queueMove")
    if queue_move:
        print(
            f"  QueueMove: target=({queue_move.get('targetX')}, {queue_move.get('targetY')})",
            flush=True,
        )

    move_req = summary.get("moveRequest")
    if move_req:
        print(
            f"  MoveRequest: from ({move_req.get('positionX')}, {move_req.get('positionY')}) "
            f"→ ({move_req.get('targetX')}, {move_req.get('targetY')})",
            flush=True,
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Compare manual map vs minimap clicks")
    parser.add_argument("-n", "--seconds", type=int, default=120, help="Observe duration")
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
        help="Seconds before you start clicking",
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

    print("", flush=True)
    print("=" * 60, flush=True)
    print("РЕЖИМ СРАВНЕНИЯ КЛИКОВ", flush=True)
    print("Кликайте по ГЛАВНОЙ КАРТЕ и по МИНИКАРТЕ (по очереди, несколько раз).", flush=True)
    print(f"Наблюдение: {args.seconds} секунд. События печатаются в реальном времени.", flush=True)
    print("=" * 60, flush=True)
    print("", flush=True)

    time.sleep(args.seconds)

    clicks = [e for e in _events if is_click_input(e)]
    # Primary click markers for summary (ignore mouse_down/up noise where possible)
    primary_sources = {
        "main_map_hero_move",
        "main_map_continuous",
        "minimap_click",
    }
    primary_clicks = [c for c in clicks if c.get("source") in primary_sources]

    print("\n" + "=" * 60, flush=True)
    print("ИТОГО", flush=True)
    print("=" * 60, flush=True)
    print(f"Всего click_input: {len(clicks)}", flush=True)

    click_stats = parse_json(script.exports_sync.get_click_compare_stats())
    print(f"clickCompareStats: {json.dumps(click_stats, ensure_ascii=False)}", flush=True)

    if not primary_clicks:
        print("Нет основных кликов (HeroMove / minimap MapClick). Кликните ещё раз.", flush=True)
    else:
        for i, click in enumerate(primary_clicks, start=1):
            print_session_summary(i, summarize_click_session(click))

    net_stats = parse_json(script.exports_sync.get_net_stats())
    print(f"\nnetStats: {json.dumps(net_stats, ensure_ascii=False)}", flush=True)

    try:
        script.exports_sync.stop()
    except Exception:
        pass

    session.detach()
    print("\nDone.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
