#!/usr/bin/env python3
"""
Attach to DarkOrbit Pepper Flash process and control the game via Frida AVM hooks.

Usage:
  python avm_move.py --pid 12345 --x 5000 --y 3000
  python avm_move.py --serve --port 44570
  python avm_move.py --status

Hybrid API (serve mode):
  WS   /ws           — push status/events (subscribe)
  GET  /status       — snapshot (debug/curl)
  GET  /methods[?object=0x...]
  POST /move         {"x":5000,"y":3000}  — accepted (queued), not completed
  POST /collect      {"x":5000,"y":3000,"collectableAdr":"0x..."}
  POST /select       {"args":[1,2,3,...]}
  POST /useItem      {"itemId":"...","methodIndex":19,"args":["0x...","0x..."]}
  POST /refine       {"refineUtilAddress":"0x...","oreId":1,"amount":100}
  POST /invoke       {"objectPtr":"0x...","methodIndex":10,"args":[...]}
  POST /callMethod   — alias for /invoke

Requires: pip install frida psutil aiohttp websockets
Launch DarkOrbit Client with --no-sandbox or Settings.NoSandbox if attach fails.
"""
from __future__ import annotations

import argparse
import sys

try:
    from avm_bridge.bridge_service import BridgeService
    from avm_bridge.models import DEFAULT_PORT
except ImportError as exc:
    print("[!] Missing dependency:", exc)
    print("    pip install frida psutil aiohttp")
    sys.exit(1)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="DarkOrbit Frida game API (AVM movement + interactions)")
    parser.add_argument("-p", "--pid", type=int, help="Pepper Flash process id")
    parser.add_argument("-x", "--x", type=float, help="Map X coordinate")
    parser.add_argument("-y", "--y", type=float, help="Map Y coordinate")
    parser.add_argument("--serve", action="store_true", help="Bridge server mode (HTTP + WS)")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help="Bridge port (default 44570)")
    parser.add_argument("--status", action="store_true", help="Print AVM status and method list")
    parser.add_argument("--wait", type=float, default=120.0, help="Seconds to wait for map load")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    pid = BridgeService.resolve_pid(args.pid)
    if not pid:
        print("[!] No Pepper Flash process found. Open the game map first.")
        return 1

    if args.serve:
        svc = BridgeService(pid, port=args.port)
        return svc.run_serve(args.wait)
    if args.status:
        return BridgeService.run_status(pid, args.wait)
    if args.x is None or args.y is None:
        print("[!] Provide --x and --y, or use --serve / --status")
        return 1
    return BridgeService.run_move(pid, args.x, args.y, args.wait)


if __name__ == "__main__":
    sys.exit(main())
