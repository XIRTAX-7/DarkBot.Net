#!/usr/bin/env python3
"""
Attach to DarkOrbit Pepper Flash process and control the game via Frida AVM hooks.

Usage:
  python avm_move.py --pid 12345 --x 5000 --y 3000
  python avm_move.py --serve --port 44570
  python avm_move.py --status

HTTP API (serve mode):
  GET  /status
  GET  /methods[?object=0x...]
  POST /move        {"x":5000,"y":3000}
  POST /collect     {"x":5000,"y":3000,"collectableAdr":"0x..."}
  POST /select      {"args":[1,2,3,...]}  — tagged int args for entity select
  POST /useItem     {"itemId":"...","methodIndex":19,"args":["0x...","0x..."]}
  POST /refine      {"refineUtilAddress":"0x...","oreId":1,"amount":100}
  POST /invoke      {"objectPtr":"0x...","methodIndex":10,"args":[...]}
  POST /callMethod  — alias for /invoke

Requires: pip install frida psutil
Launch DarkOrbit Client with --no-sandbox or Settings.NoSandbox if attach fails.
"""
from __future__ import annotations

import argparse
import ctypes
import json
import sys
import threading
import time
from ctypes import wintypes
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

try:
    import frida
    import psutil
except ImportError as exc:
    print("[!] Missing dependency:", exc)
    print("    pip install frida psutil")
    sys.exit(1)

SCRIPT_PATH = Path(__file__).with_name("avm_move_agent.js")
DEFAULT_PORT = 44570
HEAP_PROBE_SCRIPT = """
rpc.exports = {
    heap: function () {
        var out = [];
        if (typeof Process.enumerateRangesSync === 'function') {
            try {
                var rs = Process.enumerateRangesSync({ protection: 'r--', coalesce: true });
                for (var i = 0; i < rs.length; i++) {
                    out.push({ base: rs[i].base.toString(), size: rs[i].size });
                }
                return JSON.stringify(out);
            } catch (e) {}
        }
        if (typeof Process.enumerateMallocRangesSync === 'function') {
            var malloc = Process.enumerateMallocRangesSync();
            for (var j = 0; j < malloc.length; j++) {
                out.push({ base: malloc[j].base.toString(), size: malloc[j].size });
            }
        }
        return JSON.stringify(out);
    }
};
"""


def enumerate_windows_readable_ranges(pid: int) -> list[dict]:
    """Fallback when Frida 17 cannot enumerate ranges inside Pepper plugin."""
    if sys.platform != "win32":
        return []

    kernel32 = ctypes.windll.kernel32

    class MEMORY_BASIC_INFORMATION64(ctypes.Structure):
        _fields_ = [
            ("BaseAddress", ctypes.c_ulonglong),
            ("AllocationBase", ctypes.c_ulonglong),
            ("AllocationProtect", wintypes.DWORD),
            ("_pad1", wintypes.DWORD),
            ("RegionSize", ctypes.c_ulonglong),
            ("State", wintypes.DWORD),
            ("Protect", wintypes.DWORD),
            ("Type", wintypes.DWORD),
            ("_pad2", wintypes.DWORD),
        ]

    MEM_COMMIT = 0x1000
    PAGE_GUARD = 0x100
    PAGE_NOACCESS = 0x01
    READABLE_MASK = 0x02 | 0x04 | 0x08 | 0x20 | 0x40 | 0x80
    MAX_SCAN_BYTES = 64 * 1024 * 1024

    access = 0x0400 | 0x0010  # QUERY_INFORMATION | VM_READ
    handle = kernel32.OpenProcess(access, False, pid)
    if not handle:
        print(f"[!] OpenProcess(pid={pid}) failed, error={ctypes.get_last_error()}")
        return []

    ranges: list[dict] = []
    address = 0
    max_user = 0x00007FFFFFFFFFFF
    mbi = MEMORY_BASIC_INFORMATION64()

    try:
        while address < max_user:
            result = kernel32.VirtualQueryEx(
                handle,
                ctypes.c_void_p(address),
                ctypes.byref(mbi),
                ctypes.sizeof(mbi),
            )
            if result == 0:
                break

            base = int(mbi.BaseAddress)
            size = int(mbi.RegionSize)
            if size <= 0:
                address += 0x1000
                continue

            if mbi.State == MEM_COMMIT:
                protect = int(mbi.Protect)
                if (protect & PAGE_GUARD) == 0 and (protect & PAGE_NOACCESS) == 0:
                    if (protect & READABLE_MASK) != 0:
                        ranges.append({
                            "base": f"0x{base:x}",
                            "size": min(size, MAX_SCAN_BYTES),
                        })

            next_addr = base + size
            if next_addr <= address:
                address += 0x1000
            else:
                address = next_addr
    finally:
        kernel32.CloseHandle(handle)

    return ranges


def probe_heap_ranges(session: frida.core.Session) -> list[dict]:
    probe = session.create_script(HEAP_PROBE_SCRIPT)
    probe.load()
    try:
        raw = probe.exports_sync.heap()
        ranges = json.loads(raw)
        return ranges if isinstance(ranges, list) else []
    finally:
        try:
            probe.unload()
        except frida.InvalidOperationError:
            pass


def load_agent_script(session: frida.core.Session, pid: int) -> str:
    template = SCRIPT_PATH.read_text(encoding="utf-8")
    ranges = probe_heap_ranges(session)
    if ranges:
        print(f"[+] Frida heap/readable ranges: {len(ranges)}")
    else:
        ranges = enumerate_windows_readable_ranges(pid)
        print(f"[+] Windows VirtualQueryEx ranges: {len(ranges)}")
    return template.replace("/*INJECTED_HEAP_RANGES*/", json.dumps(ranges))


def find_flash_process() -> int:
    for proc in psutil.process_iter(["pid", "cmdline"]):
        try:
            maps = list(proc.memory_maps())
            has_flash = any(
                "pepflash" in (m.path or "").lower() or "flash.ocx" in (m.path or "").lower()
                for m in maps
            )
        except (psutil.AccessDenied, psutil.NoSuchProcess):
            continue

        cmdline = proc.info.get("cmdline") or []
        is_ppapi = any("--type=ppapi" in (c or "") for c in cmdline)

        if has_flash or is_ppapi:
            return int(proc.info["pid"])
    return 0


class AvmMoveSession:
    def __init__(self, pid: int) -> None:
        self.pid = pid
        self._session: frida.core.Session | None = None
        self._script: frida.core.Script | None = None
        self._last_status: dict = {}
        self._ready_event = threading.Event()

    def attach(self) -> None:
        if not SCRIPT_PATH.is_file():
            raise FileNotFoundError(f"Frida agent not found: {SCRIPT_PATH}")

        try:
            self._session = frida.attach(self.pid)
        except frida.ProcessNotFoundError as exc:
            hint = find_flash_process()
            msg = f"Process pid {self.pid} not found (client restarted?)."
            if hint:
                msg += f" Current Pepper Flash pid is {hint} — omit -p or use: -p {hint}"
            else:
                msg += " Open the game map first, then run without -p for auto-detect."
            raise RuntimeError(msg) from exc
        except frida.ProcessNotRespondingError as exc:
            raise RuntimeError(
                f"Cannot attach to pid {self.pid}: {exc}\n"
                "Try: DarkOrbit Client with Settings.NoSandbox=true or --no-sandbox"
            ) from exc

        source = load_agent_script(self._session, self.pid)
        self._script = self._session.create_script(source)
        self._script.on("message", self._on_message)
        self._script.load()

    def _on_message(self, message: dict, _data) -> None:
        if message.get("type") == "send":
            payload = message.get("payload") or {}
            print(f"[agent] {json.dumps(payload, ensure_ascii=False)}")
            if payload.get("type") == "ready":
                self._last_status = payload.get("state") or {}
                self._ready_event.set()
        elif message.get("type") == "error":
            print("[agent error]", message.get("stack", message))

    def wait_ready(self, timeout_sec: float = 120.0) -> bool:
        if self._script is None:
            raise RuntimeError("Not attached")

        deadline = time.time() + timeout_sec
        while time.time() < deadline:
            if self._script.exports_sync.is_ready():
                return True
            time.sleep(0.25)

        return False

    def status(self) -> dict:
        if self._script is None:
            return {"ready": False, "error": "not attached"}
        raw = self._script.exports_sync.get_status()
        return json.loads(raw)

    def move(self, x: float, y: float) -> dict:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        raw = self._script.exports_sync.move_to(float(x), float(y))
        return json.loads(raw)

    def list_methods(self, limit: int = 24, object_ptr: str | None = None) -> dict | list:
        if self._script is None:
            return []
        if object_ptr:
            raw = self._script.exports_sync.list_methods(object_ptr, limit)
        else:
            raw = self._script.exports_sync.list_methods(limit)
        return json.loads(raw)

    def collect(self, x: float, y: float, collectable_adr: str) -> dict:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        raw = self._script.exports_sync.collect_to(float(x), float(y), collectable_adr)
        return json.loads(raw)

    def select(self, args: list[int]) -> dict:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        raw = self._script.exports_sync.select_entity(json.dumps(args))
        return json.loads(raw)

    def use_item(self, item_id: str, method_index: int, args: list) -> dict:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        raw = self._script.exports_sync.use_item(item_id, method_index, json.dumps(args))
        return json.loads(raw)

    def refine(self, refine_util_address: str, ore_id: int, amount: int, method_index: int | None = None) -> dict:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        idx = -1 if method_index is None else int(method_index)
        raw = self._script.exports_sync.refine(refine_util_address, int(ore_id), int(amount), idx)
        return json.loads(raw)

    def invoke(self, object_ptr: str, method_index: int, args: list) -> dict:
        if self._script is None:
            return {"ok": False, "error": "not attached"}
        raw = self._script.exports_sync.invoke_method(object_ptr, int(method_index), json.dumps(args))
        return json.loads(raw)

    def detach(self) -> None:
        if self._session is not None:
            try:
                self._session.detach()
            except frida.InvalidOperationError:
                pass
        self._session = None
        self._script = None


def run_move(pid: int, x: float, y: float, wait_sec: float) -> int:
    session = AvmMoveSession(pid)
    print(f"[+] Attaching to pid {pid}...")
    session.attach()

    print(f"[+] Waiting for map load (up to {wait_sec:.0f}s)...")
    if not session.wait_ready(wait_sec):
        print("[!] Timeout — open the game map (internalMapRevolution) and retry.")
        print("    Status:", json.dumps(session.status(), indent=2))
        session.detach()
        return 2

    print("[+] Ready:", json.dumps(session.status(), indent=2))
    result = session.move(x, y)
    print("[+] Move result:", json.dumps(result, indent=2))
    session.detach()
    return 0 if result.get("ok") else 1


def run_status(pid: int, wait_sec: float) -> int:
    session = AvmMoveSession(pid)
    session.attach()
    session.wait_ready(wait_sec)
    print(json.dumps(session.status(), indent=2))
    methods = session.list_methods()
    if methods:
        for label, key in [("candidates", "candidates"), ("methods", "methods")]:
            items = methods.get(key) if isinstance(methods, dict) else None
            if items:
                print(f"\n[eventManager {key}]")
                for m in items:
                    print(f"  [{m['index']:2d}] {m['name']} params={m['params']} compiled={m['compiled']}")
    session.detach()
    return 0


def run_serve(pid: int, port: int, wait_sec: float) -> int:
    session = AvmMoveSession(pid)
    print(f"[+] Attaching to pid {pid}...")
    session.attach()
    print(f"[+] Frida game API — HTTP on :{port} (waiting for map up to {wait_sec:.0f}s)")

    class Handler(BaseHTTPRequestHandler):
        def log_message(self, fmt, *args) -> None:
            print(f"[http] {self.address_string()} - {fmt % args}")

        def _json(self, code: int, body: dict) -> None:
            data = json.dumps(body).encode("utf-8")
            self.send_response(code)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)

        def _read_json(self) -> dict:
            length = int(self.headers.get("Content-Length", 0))
            body = self.rfile.read(length) if length else b"{}"
            try:
                return json.loads(body.decode("utf-8")) if body else {}
            except json.JSONDecodeError as exc:
                raise ValueError(str(exc)) from exc

        def do_GET(self) -> None:
            path = self.path.split("?", 1)[0]
            if path.startswith("/status"):
                self._json(200, session.status())
            elif path.startswith("/methods"):
                query = self.path.split("?", 1)[1] if "?" in self.path else ""
                object_ptr = None
                if query:
                    for part in query.split("&"):
                        if part.startswith("object="):
                            object_ptr = part.split("=", 1)[1]
                self._json(200, {"methods": session.list_methods(object_ptr=object_ptr)})
            else:
                self._json(200, {
                    "endpoints": [
                        "GET /status",
                        "GET /methods[?object=0x...]",
                        "POST /move",
                        "POST /collect",
                        "POST /select",
                        "POST /useItem",
                        "POST /refine",
                        "POST /invoke",
                        "POST /callMethod",
                    ]
                })

        def do_POST(self) -> None:
            path = self.path.split("?", 1)[0]
            try:
                payload = self._read_json()
            except ValueError as exc:
                self._json(400, {"ok": False, "error": str(exc)})
                return

            if path.startswith("/move"):
                try:
                    x = float(payload["x"])
                    y = float(payload["y"])
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return
                result = session.move(x, y)
            elif path.startswith("/collect"):
                try:
                    x = float(payload["x"])
                    y = float(payload["y"])
                    collectable = str(payload["collectableAdr"])
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return
                result = session.collect(x, y, collectable)
            elif path.startswith("/select"):
                try:
                    args = payload["args"]
                    if not isinstance(args, list):
                        raise ValueError("args must be an array")
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return
                result = session.select([int(a) for a in args])
            elif path.startswith("/useItem"):
                try:
                    item_id = str(payload["itemId"])
                    method_index = int(payload.get("methodIndex", 19))
                    args = payload.get("args", [])
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return
                result = session.use_item(item_id, method_index, args)
            elif path.startswith("/refine"):
                try:
                    refine_util = str(payload["refineUtilAddress"])
                    ore_id = int(payload["oreId"])
                    amount = int(payload["amount"])
                    method_index = payload.get("methodIndex")
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return
                result = session.refine(refine_util, ore_id, amount, method_index)
            elif path.startswith("/invoke") or path.startswith("/callMethod"):
                try:
                    object_ptr = str(payload["objectPtr"])
                    method_index = int(payload["methodIndex"])
                    args = payload.get("args", [])
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return
                result = session.invoke(object_ptr, method_index, args)
            else:
                self._json(404, {"ok": False, "error": "unknown endpoint"})
                return

            self._json(200 if result.get("ok") else 500, result)

    server = HTTPServer(("127.0.0.1", port), Handler)
    server_thread = threading.Thread(target=server.serve_forever, daemon=True)
    server_thread.start()
    print(f"[+] Game API http://127.0.0.1:{port}/status")
    print(f"[+] Move: curl -X POST http://127.0.0.1:{port}/move -H \"Content-Type: application/json\" -d \"{{\\\"x\\\":10000,\\\"y\\\":6500}}\"")

    if not session._ready_event.wait(timeout=wait_sec):
        print("[!] Map not detected yet — keep playing; poll GET /status")

    if session._script and session._script.exports_sync.is_ready():
        print("[+] Game ready:", json.dumps(session.status(), indent=2))
    else:
        print("[!] Not ready yet — stay on the map and retry GET /status")

    try:
        while server_thread.is_alive():
            server_thread.join(timeout=1.0)
    except KeyboardInterrupt:
        print("\n[+] Stopping")
    finally:
        server.shutdown()
        session.detach()
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="DarkOrbit Frida game API (AVM movement + interactions)")
    parser.add_argument("-p", "--pid", type=int, help="Pepper Flash process id")
    parser.add_argument("-x", "--x", type=float, help="Map X coordinate")
    parser.add_argument("-y", "--y", type=float, help="Map Y coordinate")
    parser.add_argument("--serve", action="store_true", help="HTTP server mode")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help="HTTP port (default 44570)")
    parser.add_argument("--status", action="store_true", help="Print AVM status and method list")
    parser.add_argument("--wait", type=float, default=120.0, help="Seconds to wait for map load")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    pid = args.pid or find_flash_process()
    if not pid:
        print("[!] No Pepper Flash process found. Open the game map first.")
        return 1

    if args.serve:
        return run_serve(pid, args.port, args.wait)
    if args.status:
        return run_status(pid, args.wait)
    if args.x is None or args.y is None:
        print("[!] Provide --x and --y, or use --serve / --status")
        return 1
    return run_move(pid, args.x, args.y, args.wait)


if __name__ == "__main__":
    sys.exit(main())
