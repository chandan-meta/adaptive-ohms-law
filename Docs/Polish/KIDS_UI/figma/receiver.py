"""Receives the Figma plugin's audit, build log and PNG renders on http://localhost:8787 and writes them under
Logs/UI_V5/figma/ (so the Figma build can be checked without the Figma connector). Usage: python receiver.py"""
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", "Logs", "UI_V5", "figma"))


class Handler(BaseHTTPRequestHandler):
    def cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")

    def do_OPTIONS(self):
        self.send_response(204); self.cors(); self.end_headers()

    def do_POST(self):
        name = unquote(self.path.lstrip("/"))
        parts = [p for p in name.replace("\\", "/").split("/") if p not in ("", ".", "..")]
        path = os.path.join(OUT, *parts) if parts else None
        body = self.rfile.read(int(self.headers.get("Content-Length", 0)))
        if path and path.startswith(OUT):
            os.makedirs(os.path.dirname(path), exist_ok=True)
            open(path, "wb").write(body)
            print("saved", os.path.relpath(path, OUT), len(body), flush=True)
        self.send_response(200); self.cors(); self.end_headers(); self.wfile.write(b"ok")

    def log_message(self, *a):
        pass


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    print("receiving on http://localhost:8787 ->", OUT, flush=True)
    ThreadingHTTPServer(("127.0.0.1", 8787), Handler).serve_forever()
