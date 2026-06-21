from __future__ import annotations

import psutil


class ProcessFinder:
    """Locates the Pepper Flash / PPAPI process for Frida attach."""

    @staticmethod
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
