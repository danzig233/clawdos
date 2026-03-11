"""
clawdos_skill/client.py
Low-level HTTP client for Clawdos API.
"""

import base64
import requests
from typing import Any, Optional

from .config import ClawdosConfig


class ClawdosClient:
    """Thin wrapper around Clawdos REST API."""

    def __init__(self, config: ClawdosConfig):
        self.cfg = config
        self.session = requests.Session()
        self.session.headers.update(config.headers())

    # ── helpers ──────────────────────────────────────

    def _url(self, path: str) -> str:
        return f"{self.cfg.base_url}{path}"

    def _get(self, path: str, params: dict | None = None,
             auth: bool = True) -> requests.Response:
        headers = self.cfg.headers() if auth else {}
        return self.session.get(
            self._url(path), params=params, headers=headers,
            timeout=self.cfg.timeout,
        )

    def _post(self, path: str, payload: dict) -> requests.Response:
        return self.session.post(
            self._url(path), json=payload,
            timeout=self.cfg.timeout,
        )

    # ── 1. Health ────────────────────────────────────

    def health(self) -> dict:
        resp = self._get("/v1/health", auth=False)
        resp.raise_for_status()
        return resp.json()

    def env(self) -> dict:
        resp = self._get("/v1/env")
        resp.raise_for_status()
        return resp.json()

    # ── 2. Screen ────────────────────────────────────

    def screen_capture(
        self,
        fmt: str = "png",
        quality: int = 80,
    ) -> tuple[bytes, str]:
        """Returns (image_bytes, content_type)."""
        resp = self._get(
            "/v1/screen/capture",
            params={"format": fmt, "quality": quality},
        )
        resp.raise_for_status()
        return resp.content, resp.headers.get("Content-Type", f"image/{fmt}")

    # ── 3. Input ─────────────────────────────────────

    def click(
        self, x: int, y: int,
        button: str = "left", count: int = 1,
        capture_after_ms: int = 0,
    ) -> dict:
        return self._post("/v1/input/click", {
            "x": x, "y": y,
            "button": button, "count": count,
            "captureAfterMs": capture_after_ms,
        }).json()

    def move(self, x: int, y: int) -> dict:
        return self._post("/v1/input/move", {"x": x, "y": y}).json()

    def drag(
        self,
        from_x: int, from_y: int,
        to_x: int, to_y: int,
        button: str = "left",
        duration_ms: int = 300,
        capture_after_ms: int = 0,
    ) -> dict:
        return self._post("/v1/input/drag", {
            "fromX": from_x, "fromY": from_y,
            "toX": to_x, "toY": to_y,
            "button": button,
            "durationMs": duration_ms,
            "captureAfterMs": capture_after_ms,
        }).json()

    def keys(
        self, combo: list[str],
        capture_after_ms: int = 0,
    ) -> dict:
        return self._post("/v1/input/keys", {
            "combo": combo,
            "captureAfterMs": capture_after_ms,
        }).json()

    def type_text(
        self, text: str,
        use_clipboard: bool = False,
        capture_after_ms: int = 0,
    ) -> dict:
        return self._post("/v1/input/type", {
            "text": text,
            "useClipboard": use_clipboard,
            "captureAfterMs": capture_after_ms,
        }).json()

    def batch(
        self, actions: list[dict],
        capture_after_ms: int = 0,
    ) -> dict:
        return self._post("/v1/input/batch", {
            "actions": actions,
            "captureAfterMs": capture_after_ms,
        }).json()

    # ── 4. Window ────────────────────────────────────

    def window_list(self) -> dict:
        resp = self._get("/v1/window/list")
        resp.raise_for_status()
        return resp.json()

    def window_focus(
        self,
        title_contains: str | None = None,
        process_name: str | None = None,
    ) -> dict:
        payload: dict[str, Any] = {}
        if title_contains:
            payload["titleContains"] = title_contains
        if process_name:
            payload["processName"] = process_name
        return self._post("/v1/window/focus", payload).json()

    # ── 5. FileSystem ────────────────────────────────

    def fs_list(self, path: str, root_id: int | None = None) -> dict:
        rid = root_id if root_id is not None else self.cfg.fs_root_id
        resp = self._get("/v1/fs/list", {"rootId": rid, "path": path})
        resp.raise_for_status()
        return resp.json()

    def fs_read(self, path: str, root_id: int | None = None) -> str:
        rid = root_id if root_id is not None else self.cfg.fs_root_id
        resp = self._get("/v1/fs/read", {"rootId": rid, "path": path})
        resp.raise_for_status()
        data = resp.json()
        return base64.b64decode(data["data"]).decode("utf-8")

    def fs_write(
        self, path: str, content: str,
        overwrite: bool = True,
        root_id: int | None = None,
    ) -> dict:
        rid = root_id if root_id is not None else self.cfg.fs_root_id
        encoded = base64.b64encode(content.encode("utf-8")).decode("ascii")
        return self._post("/v1/fs/write", {
            "rootId": rid, "path": path,
            "encoding": "base64", "data": encoded,
            "overwrite": overwrite,
        }).json()

    def fs_mkdir(self, path: str, root_id: int | None = None) -> dict:
        rid = root_id if root_id is not None else self.cfg.fs_root_id
        return self._post("/v1/fs/mkdir", {
            "rootId": rid, "path": path,
        }).json()

    def fs_delete(
        self, path: str,
        recursive: bool = False,
        root_id: int | None = None,
    ) -> dict:
        rid = root_id if root_id is not None else self.cfg.fs_root_id
        return self._post("/v1/fs/delete", {
            "rootId": rid, "path": path, "recursive": recursive,
        }).json()

    def fs_move(
        self, src: str, dst: str,
        overwrite: bool = True,
        root_id: int | None = None,
    ) -> dict:
        rid = root_id if root_id is not None else self.cfg.fs_root_id
        return self._post("/v1/fs/move", {
            "rootId": rid, "from": src, "to": dst,
            "overwrite": overwrite,
        }).json()

    # ── 6. Shell ─────────────────────────────────────

    def shell_exec(
        self,
        command: str,
        args: list[str] | None = None,
        working_dir: str = "",
        timeout_sec: int = 30,
    ) -> dict:
        return self._post("/v1/shell/exec", {
            "command": command,
            "args": args or [],
            "workingDir": working_dir,
            "timeoutSec": timeout_sec,
        }).json()