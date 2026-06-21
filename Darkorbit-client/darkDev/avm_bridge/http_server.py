from __future__ import annotations

import json
from http.server import BaseHTTPRequestHandler, HTTPServer
from threading import Thread
from typing import TYPE_CHECKING
from urllib.parse import parse_qs, urlparse

if TYPE_CHECKING:
    from avm_bridge.action_dispatcher import ActionDispatcher
    from avm_bridge.game_state_store import GameStateStore


class HttpServer:
    """Legacy HTTP: GET /status (debug), POST commands via ActionDispatcher."""

    def __init__(
        self,
        port: int,
        store: GameStateStore,
        dispatcher: ActionDispatcher,
    ) -> None:
        self._port = port
        self._store = store
        self._dispatcher = dispatcher
        self._httpd: HTTPServer | None = None
        self._thread: Thread | None = None

    def start(self) -> None:
        store = self._store
        dispatcher = self._dispatcher

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
                path = urlparse(self.path).path
                if path.startswith("/status"):
                    self._json(200, store.snapshot())
                elif path.startswith("/methods"):
                    query = parse_qs(urlparse(self.path).query)
                    object_ptr = query.get("object", [None])[0]
                    self._json(200, {"methods": dispatcher.list_methods(object_ptr=object_ptr)})
                else:
                    self._json(200, {
                        "endpoints": [
                            "WS /ws",
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
                path = urlparse(self.path).path.lstrip("/")
                action_map = {
                    "move": "move",
                    "collect": "collect",
                    "select": "select",
                    "useItem": "useItem",
                    "refine": "refine",
                    "invoke": "invoke",
                    "callMethod": "callMethod",
                }
                action = action_map.get(path.split("/")[0] if path else "")
                if not action:
                    self._json(404, {"ok": False, "error": "unknown endpoint"})
                    return

                try:
                    payload = self._read_json()
                    result = dispatcher.dispatch(action, payload)
                except (KeyError, ValueError, TypeError) as exc:
                    self._json(400, {"ok": False, "error": str(exc)})
                    return

                code = 202 if result.accepted else (500 if not result.ok else 200)
                self._json(code, result.to_dict())

        self._httpd = HTTPServer(("127.0.0.1", self._port), Handler)
        self._thread = Thread(target=self._httpd.serve_forever, daemon=True, name="http-bridge")
        self._thread.start()

    def stop(self) -> None:
        if self._httpd is not None:
            self._httpd.shutdown()
            self._httpd = None
