from __future__ import annotations

import ctypes
import json
import sys
from ctypes import wintypes
from pathlib import Path

import frida

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


class HeapRangeLoader:
    """Loads readable memory ranges for AVM pattern scan injection."""

    def __init__(self, agent_script_path: Path) -> None:
        self._agent_script_path = agent_script_path

    def load_agent_source(self, session: frida.core.Session, pid: int) -> str:
        template = self._agent_script_path.read_text(encoding="utf-8")
        ranges = self._probe_heap_ranges(session)
        if ranges:
            print(f"[+] Frida heap/readable ranges: {len(ranges)}")
        else:
            ranges = self._enumerate_windows_readable_ranges(pid)
            print(f"[+] Windows VirtualQueryEx ranges: {len(ranges)}")
        return template.replace("/*INJECTED_HEAP_RANGES*/", json.dumps(ranges))

    @staticmethod
    def _probe_heap_ranges(session: frida.core.Session) -> list[dict]:
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

    @staticmethod
    def _enumerate_windows_readable_ranges(pid: int) -> list[dict]:
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

        mem_commit = 0x1000
        page_guard = 0x100
        page_noaccess = 0x01
        readable_mask = 0x02 | 0x04 | 0x08 | 0x20 | 0x40 | 0x80
        max_scan_bytes = 64 * 1024 * 1024

        access = 0x0400 | 0x0010
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

                if mbi.State == mem_commit:
                    protect = int(mbi.Protect)
                    if (protect & page_guard) == 0 and (protect & page_noaccess) == 0:
                        if (protect & readable_mask) != 0:
                            ranges.append({
                                "base": f"0x{base:x}",
                                "size": min(size, max_scan_bytes),
                            })

                next_addr = base + size
                address = address + 0x1000 if next_addr <= address else next_addr
        finally:
            kernel32.CloseHandle(handle)

        return ranges
