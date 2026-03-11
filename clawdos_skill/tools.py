"""
clawdos_skill/tools.py
All 18 OpenClaw tool definitions for Clawdos desktop automation.
"""

import base64
from openclaw import tool, ToolResult, ImageContent
from .client import ClawdosClient


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  1. Health
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

@tool(
    name="health_check",
    description="Check whether the Clawdos service is online. Returns version and uptime. No auth required.",
)
def health_check(client: ClawdosClient) -> ToolResult:
    data = client.health()
    return ToolResult(
        output=(
            f"Clawdos is {'online' if data.get('ok') else 'offline'}\n"
            f"Version : {data.get('version', 'N/A')}\n"
            f"Uptime  : {data.get('uptimeMs', 0) / 1000:.1f}s"
        ),
        metadata=data,
    )


@tool(
    name="get_env",
    description="Get host environment info: screen resolution, DPI scale, active window, and IME state.",
)
def get_env(client: ClawdosClient) -> ToolResult:
    data = client.env()
    return ToolResult(
        output=(
            f"Screen  : {data['screenWidth']}×{data['screenHeight']}\n"
            f"DPI     : {data.get('dpiScale', 1.0)}\n"
            f"Active  : {data.get('activeWindow', 'N/A')}\n"
            f"IME     : {data.get('imeEnabled', 'N/A')}"
        ),
        metadata=data,
    )


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  2. Screen
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

@tool(
    name="screen_capture",
    description="Capture the current screen of the Clawdos host. Returns an image for visual understanding and UI locating.",
    parameters={
        "format": {
            "type": "string", "enum": ["png", "jpg"],
            "default": "png", "description": "Image format",
        },
        "quality": {
            "type": "integer", "default": 80,
            "description": "JPEG quality (1-100), only applies to jpg format",
        },
    },
)
def screen_capture(
    client: ClawdosClient,
    format: str = "png",
    quality: int = 80,
) -> ToolResult:
    img_bytes, content_type = client.screen_capture(fmt=format, quality=quality)
    b64 = base64.b64encode(img_bytes).decode("ascii")
    return ToolResult(
        output=f"Screen captured ({len(img_bytes)} bytes, {content_type})",
        images=[ImageContent(data=b64, media_type=content_type)],
    )


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  3. Input
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

@tool(
    name="mouse_click",
    description="Perform a mouse click at the given coordinates (supports left/right/middle button, single/double click).",
    parameters={
        "x": {"type": "integer", "required": True, "description": "X coordinate"},
        "y": {"type": "integer", "required": True, "description": "Y coordinate"},
        "button": {
            "type": "string", "enum": ["left", "right", "middle"],
            "default": "left", "description": "Mouse button",
        },
        "count": {"type": "integer", "default": 1, "description": "Click count"},
    },
)
def mouse_click(
    client: ClawdosClient,
    x: int, y: int,
    button: str = "left", count: int = 1,
) -> ToolResult:
    data = client.click(x, y, button=button, count=count, capture_after_ms=200)
    return ToolResult(output=f"Clicked ({x}, {y}) button={button} count={count}", metadata=data)


@tool(
    name="mouse_move",
    description="Move the mouse cursor to the given coordinates.",
    parameters={
        "x": {"type": "integer", "required": True},
        "y": {"type": "integer", "required": True},
    },
)
def mouse_move(client: ClawdosClient, x: int, y: int) -> ToolResult:
    data = client.move(x, y)
    return ToolResult(output=f"Moved cursor to ({x}, {y})", metadata=data)


@tool(
    name="mouse_drag",
    description="Drag from one coordinate to another.",
    parameters={
        "from_x": {"type": "integer", "required": True},
        "from_y": {"type": "integer", "required": True},
        "to_x":   {"type": "integer", "required": True},
        "to_y":   {"type": "integer", "required": True},
        "button": {"type": "string", "default": "left"},
        "duration_ms": {"type": "integer", "default": 300, "description": "Drag duration in milliseconds"},
    },
)
def mouse_drag(
    client: ClawdosClient,
    from_x: int, from_y: int,
    to_x: int, to_y: int,
    button: str = "left", duration_ms: int = 300,
) -> ToolResult:
    data = client.drag(from_x, from_y, to_x, to_y, button=button, duration_ms=duration_ms)
    return ToolResult(output=f"Dragged ({from_x},{from_y}) → ({to_x},{to_y})", metadata=data)


@tool(
    name="key_combo",
    description="Send a keyboard combo, e.g. ['CTRL', 'C'] for Ctrl+C.",
    parameters={
        "combo": {
            "type": "array", "items": {"type": "string"},
            "required": True,
            "description": "List of keys, e.g. ['CTRL', 'SHIFT', 'S']",
        },
    },
)
def key_combo(client: ClawdosClient, combo: list[str]) -> ToolResult:
    data = client.keys(combo, capture_after_ms=100)
    return ToolResult(output=f"Pressed {'+'.join(combo)}", metadata=data)


@tool(
    name="type_text",
    description="Type text. ASCII text is typed directly; non-ASCII (e.g. Chinese) auto-uses clipboard paste.",
    parameters={
        "text": {"type": "string", "required": True, "description": "Text to type"},
        "use_clipboard": {
            "type": "boolean", "default": False,
            "description": "Force clipboard paste (auto-enabled for non-ASCII)",
        },
    },
)
def type_text(
    client: ClawdosClient,
    text: str, use_clipboard: bool = False,
) -> ToolResult:
    needs_clipboard = use_clipboard or any(ord(c) > 127 for c in text)
    data = client.type_text(text, use_clipboard=needs_clipboard, capture_after_ms=200)
    return ToolResult(output=f"Typed {len(text)} chars (clipboard={needs_clipboard})", metadata=data)


@tool(
    name="input_batch",
    description="Execute multiple input actions (move/click/drag/keys/type/wait) sequentially in a single batch.",
    parameters={
        "actions": {
            "type": "array", "required": True,
            "description": (
                "List of actions. Each action is a dict with a required 'type' field.\n"
                "Supported types: move, click, drag, keys, type, wait\n"
                'Example: [{"type": "click", "x": 100, "y": 200}]'
            ),
        },
    },
)
def input_batch(client: ClawdosClient, actions: list[dict]) -> ToolResult:
    data = client.batch(actions, capture_after_ms=300)
    return ToolResult(
        output=f"Executed {data.get('executedCount', len(actions))} actions",
        metadata=data,
    )


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  4. Window
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

@tool(
    name="window_list",
    description="List all visible windows on the host with title, process name, and geometry.",
)
def window_list(client: ClawdosClient) -> ToolResult:
    data = client.window_list()
    windows = data.get("windows", [])
    lines = [f"Found {len(windows)} visible windows:"]
    for w in windows:
        lines.append(
            f"  • [{w.get('processName', '?')}] {w.get('title', '(untitled)')}"
            f"  pos=({w.get('x', '?')},{w.get('y', '?')}) "
            f"size={w.get('width', '?')}×{w.get('height', '?')}"
        )
    return ToolResult(output="\n".join(lines), metadata=data)


@tool(
    name="window_focus",
    description="Bring a window to the foreground by matching title keyword or process name.",
    parameters={
        "title_contains": {
            "type": "string", "default": None,
            "description": "Substring to match in window title",
        },
        "process_name": {
            "type": "string", "default": None,
            "description": "Process name (e.g. notepad, chrome)",
        },
    },
)
def window_focus(
    client: ClawdosClient,
    title_contains: str | None = None,
    process_name: str | None = None,
) -> ToolResult:
    if not title_contains and not process_name:
        return ToolResult(output="Error: at least one of title_contains or process_name is required", error=True)
    data = client.window_focus(title_contains=title_contains, process_name=process_name)
    return ToolResult(output=f"Focused window: {title_contains or process_name}", metadata=data)


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  5. FileSystem
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

@tool(
    name="fs_list",
    description="List files and subdirectories under the given path.",
    parameters={
        "path": {"type": "string", "required": True, "description": "Relative path"},
        "root_id": {"type": "integer", "default": None, "description": "Working directory ID"},
    },
)
def fs_list(client: ClawdosClient, path: str, root_id: int | None = None) -> ToolResult:
    data = client.fs_list(path, root_id=root_id)
    entries = data.get("entries", [])
    lines = [f"Directory: {path}  ({len(entries)} entries)"]
    for e in entries:
        icon = "📁" if e["type"] == "dir" else "📄"
        lines.append(f"  {icon} {e['name']}  ({e.get('size', 0)} bytes)")
    return ToolResult(output="\n".join(lines), metadata=data)


@tool(
    name="fs_read",
    description="Read the text content of a file (UTF-8).",
    parameters={
        "path": {"type": "string", "required": True},
        "root_id": {"type": "integer", "default": None},
    },
)
def fs_read(client: ClawdosClient, path: str, root_id: int | None = None) -> ToolResult:
    content = client.fs_read(path, root_id=root_id)
    return ToolResult(output=content)


@tool(
    name="fs_write",
    description="Write text content to a file.",
    parameters={
        "path": {"type": "string", "required": True},
        "content": {"type": "string", "required": True, "description": "Text content to write"},
        "overwrite": {"type": "boolean", "default": True},
        "root_id": {"type": "integer", "default": None},
    },
)
def fs_write(
    client: ClawdosClient, path: str, content: str,
    overwrite: bool = True, root_id: int | None = None,
) -> ToolResult:
    data = client.fs_write(path, content, overwrite=overwrite, root_id=root_id)
    return ToolResult(output=f"Written {len(content)} chars to {path}", metadata=data)


@tool(
    name="fs_mkdir",
    description="Create a directory (supports recursive creation).",
    parameters={
        "path": {"type": "string", "required": True},
        "root_id": {"type": "integer", "default": None},
    },
)
def fs_mkdir(client: ClawdosClient, path: str, root_id: int | None = None) -> ToolResult:
    data = client.fs_mkdir(path, root_id=root_id)
    return ToolResult(output=f"Created directory: {path}", metadata=data)


@tool(
    name="fs_delete",
    description="Delete a file or directory. Set recursive=True to remove directories recursively.",
    parameters={
        "path": {"type": "string", "required": True},
        "recursive": {"type": "boolean", "default": False},
        "root_id": {"type": "integer", "default": None},
    },
)
def fs_delete(
    client: ClawdosClient, path: str,
    recursive: bool = False, root_id: int | None = None,
) -> ToolResult:
    data = client.fs_delete(path, recursive=recursive, root_id=root_id)
    return ToolResult(output=f"Deleted: {path} (recursive={recursive})", metadata=data)


@tool(
    name="fs_move",
    description="Move or rename a file or directory.",
    parameters={
        "src": {"type": "string", "required": True, "description": "Source path"},
        "dst": {"type": "string", "required": True, "description": "Destination path"},
        "overwrite": {"type": "boolean", "default": True},
        "root_id": {"type": "integer", "default": None},
    },
)
def fs_move(
    client: ClawdosClient, src: str, dst: str,
    overwrite: bool = True, root_id: int | None = None,
) -> ToolResult:
    data = client.fs_move(src, dst, overwrite=overwrite, root_id=root_id)
    return ToolResult(output=f"Moved: {src} → {dst}", metadata=data)


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  6. Shell
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

@tool(
    name="shell_exec",
    description=(
        "Execute a shell command on the Clawdos host. "
        "Commands are restricted by a whitelist; unlisted commands return 403. "
        "Returns stdout, stderr, and exit code."
    ),
    parameters={
        "command": {
            "type": "string", "required": True,
            "description": "Command to execute (e.g. echo, dir, python)",
        },
        "args": {
            "type": "array", "items": {"type": "string"},
            "default": [], "description": "Command arguments",
        },
        "working_dir": {
            "type": "string", "default": "",
            "description": "Working directory (empty = default workingDirs[0])",
        },
        "timeout_sec": {
            "type": "integer", "default": 30,
            "description": "Timeout in seconds",
        },
    },
)
def shell_exec(
    client: ClawdosClient,
    command: str, args: list[str] | None = None,
    working_dir: str = "", timeout_sec: int = 30,
) -> ToolResult:
    data = client.shell_exec(
        command, args=args,
        working_dir=working_dir, timeout_sec=timeout_sec,
    )
    exit_code = data.get("exitCode", -1)
    stdout = data.get("stdout", "").strip()
    stderr = data.get("stderr", "").strip()
    elapsed = data.get("elapsedMs", 0)

    output_parts = [f"Exit code: {exit_code}  ({elapsed}ms)"]
    if stdout:
        output_parts.append(f"stdout:\n{stdout}")
    if stderr:
        output_parts.append(f"stderr:\n{stderr}")

    return ToolResult(
        output="\n".join(output_parts),
        metadata=data,
        error=exit_code != 0,
    )