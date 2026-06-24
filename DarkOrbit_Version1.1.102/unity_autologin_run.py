#!/usr/bin/env python3
"""HTTP login + Frida bootstrapSession + WebView autologin для уже запущенного DarkOrbit.exe."""

from __future__ import annotations

import json
import re
import sys
import time
from http.cookiejar import CookieJar
from pathlib import Path
from urllib.parse import urlencode
from urllib.request import HTTPCookieProcessor, Request, build_opener

import frida
import psutil

AGENT_PATH = Path(__file__).with_name("unity_bridge_agent.js")
USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
)
LOGIN_FORM_RE = re.compile(r'"bgcdw_login_form" action="(.*)"')
FLASH_EMBED_RE = re.compile(r'"src":\s*"([^"]*)".*?},\s*(\{.*\})', re.DOTALL)
DOSID_DOMAIN_RE = re.compile(r".*\d+.*")


def find_darkorbit_pid() -> int | None:
    for proc in psutil.process_iter(["pid", "name"]):
        if proc.info["name"] == "DarkOrbit.exe":
            return proc.info["pid"]
    return None


def http_login(username: str, password: str) -> tuple[str, str]:
    jar = CookieJar()
    opener = build_opener(HTTPCookieProcessor(jar))

    front = opener.open(
        Request("https://www.darkorbit.com/", headers={"User-Agent": USER_AGENT})
    ).read().decode("utf-8", errors="replace")

    match = LOGIN_FORM_RE.search(front)
    if not match:
        raise RuntimeError("login form not found on front page")

    login_url = match.group(1).replace("&amp;", "&")
    data = urlencode({"username": username, "password": password}).encode()
    opener.open(
        Request(
            login_url,
            data=data,
            method="POST",
            headers={
                "User-Agent": USER_AGENT,
                "Content-Type": "application/x-www-form-urlencoded",
            },
        )
    ).read()

    for cookie in jar:
        if cookie.name.lower() == "dosid" and DOSID_DOMAIN_RE.match(cookie.domain):
            domain = cookie.domain.lstrip(".")
            return cookie.value, domain

    raise RuntimeError("dosid cookie not found after login")


def fetch_fallback_json(instance_host: str, dosid: str) -> str | None:
    opener = build_opener()
    headers = {"User-Agent": USER_AGENT, "Cookie": f"dosid={dosid}"}
    attempts = [
        f"https://{instance_host}/indexInternal.es?action=internalWebGL",
        f"https://{instance_host}/indexInternal.es?action=internalMapRevolution",
    ]

    for url in attempts:
        body = opener.open(Request(url, headers=headers)).read().decode("utf-8", errors="replace")
        trimmed = body.strip()
        if trimmed.startswith("{"):
            return trimmed

        for line in body.split("\n"):
            if "flashembed(" not in line:
                continue
            match = FLASH_EMBED_RE.search(line) or FLASH_EMBED_RE.search(body)
            if match:
                return match.group(2)

    return None


def on_message_factory(events: list[dict]):
    def on_message(message, _data):
        if message.get("type") == "error":
            print("[frida-error]", message.get("stack", message), flush=True)
            return
        payload = message.get("payload")
        if isinstance(payload, dict):
            events.append(payload)
            t = payload.get("type", "?")
            if t != "ping":
                print(json.dumps(payload, ensure_ascii=False), flush=True)
        elif isinstance(payload, str):
            print(payload, flush=True)

    return on_message


def main() -> int:
    username = "QuantumRavager"
    password = "Xirtax456852"
    pid = find_darkorbit_pid()
    wait_sec = 120

    if len(sys.argv) > 1:
        pid = int(sys.argv[1])
    if len(sys.argv) > 2:
        wait_sec = int(sys.argv[2])

    if not pid:
        print("DarkOrbit.exe not found", file=sys.stderr)
        return 1

    print("HTTP login...", flush=True)
    dosid, instance = http_login(username, password)
    print(f"dosid suffix={dosid[-4:]}, instance={instance}", flush=True)

    web_gl_json = fetch_fallback_json(instance, dosid)
    print(f"fallback JSON: {len(web_gl_json) if web_gl_json else 0} bytes", flush=True)

    source = AGENT_PATH.read_text(encoding="utf-8")
    events: list[dict] = []

    print(f"Frida attach pid={pid}...", flush=True)
    session = frida.attach(pid)
    script = session.create_script(source)
    script.on("message", on_message_factory(events))
    script.load()
    time.sleep(1.5)

    boot = script.exports_sync.bootstrap_session(dosid, web_gl_json or "", username, password)
    print(f"bootstrapSession: {boot}", flush=True)

    deadline = time.time() + wait_sec
    last_status = ""
    success = False
    while time.time() < deadline:
        status = script.exports_sync.get_status()
        if status != last_status:
            print(f"status: {status}", flush=True)
            last_status = status
            try:
                data = json.loads(status)
                if data.get("sessionInjected"):
                    print("SUCCESS: session injected", flush=True)
                    success = True
                    break
            except json.JSONDecodeError:
                pass
        time.sleep(3)

    try:
        script.exports_sync.stop()
    except Exception:
        pass
    session.detach()

    types = [e.get("type") for e in events]
    print("events:", " -> ".join(types[-25:]), flush=True)
    return 0 if success or any(e.get("type") == "session_injected" for e in events) else 1


if __name__ == "__main__":
    raise SystemExit(main())
