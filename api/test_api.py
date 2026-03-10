"""
Clawdos API — Python client & test script
Covers all 17 endpoints
"""

import requests
import base64
import json
import time
import os
from typing import Optional


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# Configuration
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

BASE_URL = "http://127.0.0.1:17171"  # same as clawdos-config.json
API_KEY = "your-secret-key-here"         # same as clawdos-config.json

HEADERS = {"X-Api-Key": API_KEY}


def log_result(name: str, resp: requests.Response):
    """Unified test result logging"""
    status = "✅" if resp.status_code < 400 else "❌"
    print(f"{status} [{resp.status_code}] {name}")
    try:
        print(f"   ↳ {json.dumps(resp.json(), ensure_ascii=False, indent=2)[:500]}")
    except Exception:
        print(f"   ↳ (binary/non-json, {len(resp.content)} bytes)")
    print()


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 1. Health module
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def test_health():
    """GET /v1/health — Health check (no auth required)"""
    resp = requests.get(f"{BASE_URL}/v1/health")
    log_result("GET /v1/health", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["ok"] is True
    assert "version" in data
    assert "uptimeMs" in data
    return data


def test_env():
    """GET /v1/env — Environment info (resolution, DPI, active window, IME)"""
    resp = requests.get(f"{BASE_URL}/v1/env", headers=HEADERS)
    log_result("GET /v1/env", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert "screenWidth" in data
    assert "screenHeight" in data
    assert "dpiScale" in data
    return data


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 2. Screen module
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def test_screen_capture_png():
    """GET /v1/screen/capture — Screen capture (PNG format)"""
    resp = requests.get(
        f"{BASE_URL}/v1/screen/capture",
        headers=HEADERS,
        params={"format": "png"},
    )
    log_result("GET /v1/screen/capture (png)", resp)
    assert resp.status_code == 200
    assert resp.headers["Content-Type"] == "image/png"
    # Optional: save locally for verification
    with open("capture_test.png", "wb") as f:
        f.write(resp.content)
    print("   📸 Saved capture_test.png\n")
    return resp.content


def test_screen_capture_jpg():
    """GET /v1/screen/capture — Screen capture (JPEG format, custom quality)"""
    resp = requests.get(
        f"{BASE_URL}/v1/screen/capture",
        headers=HEADERS,
        params={"format": "jpg", "quality": 60},
    )
    log_result("GET /v1/screen/capture (jpg, q=60)", resp)
    assert resp.status_code == 200
    assert resp.headers["Content-Type"] == "image/jpeg"
    return resp.content


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 3. Input module
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def test_input_click():
    """POST /v1/input/click — Mouse click"""
    payload = {
        "x": 500,
        "y": 500,
        "button": "left",
        "count": 1,
        "captureAfterMs": 200,
    }
    resp = requests.post(f"{BASE_URL}/v1/input/click", json=payload, headers=HEADERS)
    log_result("POST /v1/input/click", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["ok"] is True
    return data


def test_input_move():
    """POST /v1/input/move — Mouse move"""
    payload = {"x": 300, "y": 300}
    resp = requests.post(f"{BASE_URL}/v1/input/move", json=payload, headers=HEADERS)
    log_result("POST /v1/input/move", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_input_drag():
    """POST /v1/input/drag — Mouse drag"""
    payload = {
        "fromX": 200,
        "fromY": 200,
        "toX": 600,
        "toY": 400,
        "button": "left",
        "durationMs": 500,
        "captureAfterMs": 300,
    }
    resp = requests.post(f"{BASE_URL}/v1/input/drag", json=payload, headers=HEADERS)
    log_result("POST /v1/input/drag", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_input_keys():
    """POST /v1/input/keys — Key combination"""
    payload = {
        "combo": ["CTRL", "A"],
        "captureAfterMs": 100,
    }
    resp = requests.post(f"{BASE_URL}/v1/input/keys", json=payload, headers=HEADERS)
    log_result("POST /v1/input/keys", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_input_type_ascii():
    """POST /v1/input/type — ASCII text input"""
    payload = {
        "text": "Hello Clawdos!",
        "useClipboard": False,
        "captureAfterMs": 200,
    }
    resp = requests.post(f"{BASE_URL}/v1/input/type", json=payload, headers=HEADERS)
    log_result("POST /v1/input/type (ASCII)", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_input_type_chinese():
    """POST /v1/input/type — Chinese text input (uses clipboard automatically)"""
    payload = {
        "text": "你好，Clawdos！",
        "useClipboard": True,
        "captureAfterMs": 200,
    }
    resp = requests.post(f"{BASE_URL}/v1/input/type", json=payload, headers=HEADERS)
    log_result("POST /v1/input/type (中文)", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_input_batch():
    """POST /v1/input/batch — Batch actions"""
    payload = {
        "actions": [
            {"type": "move", "x": 100, "y": 100},
            {"type": "wait", "ms": 200},
            {"type": "click", "x": 100, "y": 100, "button": "left", "count": 1},
            {"type": "wait", "ms": 100},
            {"type": "keys", "combo": ["CTRL", "A"]},
            {"type": "type", "text": "batch test", "useClipboard": False},
            {"type": "drag", "fromX": 100, "fromY": 100, "toX": 400, "toY": 400, "durationMs": 200},
        ],
        "captureAfterMs": 300,
    }
    resp = requests.post(f"{BASE_URL}/v1/input/batch", json=payload, headers=HEADERS)
    log_result("POST /v1/input/batch", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["ok"] is True
    assert data["executedCount"] == len(payload["actions"])
    return data


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 4. Window module
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def test_window_list():
    """GET /v1/window/list — List visible windows"""
    resp = requests.get(f"{BASE_URL}/v1/window/list", headers=HEADERS)
    log_result("GET /v1/window/list", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert "windows" in data
    print(f"   🪟 Total {len(data['windows'])} visible windows\n")
    return data


def test_window_focus():
    """POST /v1/window/focus — Focus a window"""
    # For testing, we take the first visible window and focus it by partial title match
    windows = test_window_list()["windows"]
    if not windows:
        print("   ⚠️ No visible windows, skipping focus test\n")
        return None

    target_title = windows[0]["title"]
    payload = {"titleContains": target_title[:20]}
    resp = requests.post(f"{BASE_URL}/v1/window/focus", json=payload, headers=HEADERS)
    log_result("POST /v1/window/focus", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_window_focus_by_process():
    """POST /v1/window/focus — Focus by process name"""
    payload = {"processName": "explorer"}
    resp = requests.post(f"{BASE_URL}/v1/window/focus", json=payload, headers=HEADERS)
    log_result("POST /v1/window/focus (by process)", resp)
    # explorer may or may not be running, so we accept 200 or 404
    return resp.json() if resp.status_code == 200 else None


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 5. FileSystem module
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

FS_ROOT_ID = 0  # 对应 workingDirs[0]，如 C:\OpenClawWorking


def test_fs_mkdir():
    """POST /v1/fs/mkdir — Create directory"""
    payload = {"rootId": FS_ROOT_ID, "path": "test_dir/sub_dir"}
    resp = requests.post(f"{BASE_URL}/v1/fs/mkdir", json=payload, headers=HEADERS)
    log_result("POST /v1/fs/mkdir", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_fs_write():
    """POST /v1/fs/write — Write file (base64 encoded)"""
    content = "Hello from Clawdos API test! 你好世界！"
    encoded = base64.b64encode(content.encode("utf-8")).decode("ascii")
    payload = {
        "rootId": FS_ROOT_ID,
        "path": "test_dir/hello.txt",
        "encoding": "base64",
        "data": encoded,
        "overwrite": True,
    }
    resp = requests.post(f"{BASE_URL}/v1/fs/write", json=payload, headers=HEADERS)
    log_result("POST /v1/fs/write", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_fs_list():
    """GET /v1/fs/list — List directory contents"""
    resp = requests.get(
        f"{BASE_URL}/v1/fs/list",
        headers=HEADERS,
        params={"rootId": FS_ROOT_ID, "path": "test_dir"},
    )
    log_result("GET /v1/fs/list", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert "entries" in data
    for entry in data["entries"]:
        print(f"   📄 {entry['type']:4s}  {entry['name']}  ({entry['size']} bytes)")
    print()
    return data


def test_fs_read():
    """GET /v1/fs/read — Read file (returns base64)"""
    resp = requests.get(
        f"{BASE_URL}/v1/fs/read",
        headers=HEADERS,
        params={"rootId": FS_ROOT_ID, "path": "test_dir/hello.txt"},
    )
    log_result("GET /v1/fs/read", resp)
    assert resp.status_code == 200
    data = resp.json()
    decoded = base64.b64decode(data["data"]).decode("utf-8")
    print(f"   📖 File content: {decoded}\n")
    return decoded


def test_fs_move():
    """POST /v1/fs/move — Move/rename file"""
    payload = {
        "rootId": FS_ROOT_ID,
        "from": "test_dir/hello.txt",
        "to": "test_dir/hello_moved.txt",
        "overwrite": True,
    }
    resp = requests.post(f"{BASE_URL}/v1/fs/move", json=payload, headers=HEADERS)
    log_result("POST /v1/fs/move", resp)
    assert resp.status_code == 200
    assert resp.json()["ok"] is True
    return resp.json()


def test_fs_delete():
    """POST /v1/fs/delete — Delete file/directory"""
    # Delete single file first
    payload_file = {
        "rootId": FS_ROOT_ID,
        "path": "test_dir/hello_moved.txt",
        "recursive": False,
    }
    resp1 = requests.post(f"{BASE_URL}/v1/fs/delete", json=payload_file, headers=HEADERS)
    log_result("POST /v1/fs/delete (file)", resp1)

    # Then recursively delete the whole directory
    payload_dir = {
        "rootId": FS_ROOT_ID,
        "path": "test_dir",
        "recursive": True,
    }
    resp2 = requests.post(f"{BASE_URL}/v1/fs/delete", json=payload_dir, headers=HEADERS)
    log_result("POST /v1/fs/delete (dir recursive)", resp2)
    return resp2.json()


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 6. Shell module
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
def test_shell_exec_echo():
    """POST /v1/shell/exec — 执行简单 echo 命令"""
    payload = {
        "command": "echo",
        "args": ["Hello from Clawdos Shell!"],
        "timeoutSec": 10,
    }
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (echo)", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["exitCode"] == 0
    assert "Hello from Clawdos Shell!" in data["stdout"]
    print(f"   📟 stdout: {data['stdout'].strip()}")
    print(f"   ⏱️  elapsed: {data['elapsedMs']}ms\n")
    return data

def test_shell_exec_with_workdir():
    """POST /v1/shell/exec — 带工作目录的命令"""
    payload = {
        "command": "dir" if os.name == "nt" else "ls",
        "args": [],
        "workingDir": "",  # 留空使用默认 workingDirs[0]
        "timeoutSec": 10,
    }
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (dir/ls with workdir)", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["exitCode"] is not None
    return data

def test_shell_exec_multiarg():
    """POST /v1/shell/exec — 多参数命令 (ping -n 1)"""
    payload = {
        "command": "ping",
        "args": ["-n", "1", "127.0.0.1"] if os.name == "nt" else ["-c", "1", "127.0.0.1"],
        "timeoutSec": 15,
    }
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (ping)", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["exitCode"] == 0
    return data

def test_shell_exec_stderr():
    """POST /v1/shell/exec — 触发 stderr 输出（执行不存在的子命令）"""
    # 用 cmd /c 执行一个不存在的命令来产生 stderr
    if os.name == "nt":
        payload = {
            "command": "cmd",
            "args": ["/c", "nonexistent_command_xyz"],
            "timeoutSec": 10,
        }
    else:
        payload = {
            "command": "python",
            "args": ["-c", "import sys; sys.stderr.write('test stderr'); sys.exit(1)"],
            "timeoutSec": 10,
        }
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (stderr)", resp)
    assert resp.status_code == 200
    data = resp.json()
    assert data["exitCode"] != 0 or len(data.get("stderr", "")) > 0
    print(f"   ⚠️  stderr: {data.get('stderr', '').strip()[:200]}\n")
    return data

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 7. Negative tests (error handling validation)
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def test_auth_rejected():
    """Verify 401 is returned when API key is missing"""
    resp = requests.get(f"{BASE_URL}/v1/env")  # no headers
    log_result("GET /v1/env (no API key)", resp)
    assert resp.status_code == 401


def test_click_out_of_bounds():
    """Verify out-of-bounds coordinates return 400"""
    payload = {"x": -1, "y": -1}
    resp = requests.post(f"{BASE_URL}/v1/input/click", json=payload, headers=HEADERS)
    log_result("POST /v1/input/click (out of bounds)", resp)
    assert resp.status_code == 400


def test_fs_path_escape():
    """Verify path traversal is rejected with 403"""
    resp = requests.get(
        f"{BASE_URL}/v1/fs/list",
        headers=HEADERS,
        params={"rootId": FS_ROOT_ID, "path": "../../etc"},
    )
    log_result("GET /v1/fs/list (path escape)", resp)
    assert resp.status_code == 403


def test_window_focus_missing_params():
    """Verify missing parameters return 400"""
    payload = {}  # titleContains and processName are both optional, but at least one must be provided
    resp = requests.post(f"{BASE_URL}/v1/window/focus", json=payload, headers=HEADERS)
    log_result("POST /v1/window/focus (no params)", resp)
    assert resp.status_code == 400

def test_shell_blocked_command():
    """验证不在白名单的命令返回 403"""
    payload = {
        "command": "rm",  # 不在默认允许列表中
        "args": ["-rf", "/"],
        "timeoutSec": 5,
    }
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (blocked cmd)", resp)
    assert resp.status_code == 403

def test_shell_empty_command():
    """验证空命令返回 403"""
    payload = {"command": "", "args": []}
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (empty cmd)", resp)
    assert resp.status_code == 403

def test_shell_workdir_escape():
    """验证工作目录逃逸返回 403"""
    payload = {
        "command": "echo",
        "args": ["escape test"],
        "workingDir": "../../etc",
        "timeoutSec": 5,
    }
    resp = requests.post(f"{BASE_URL}/v1/shell/exec", json=payload, headers=HEADERS)
    log_result("POST /v1/shell/exec (workdir escape)", resp)
    assert resp.status_code == 403

# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# Main test runner
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def run_all():
    print("=" * 60)
    print("  Clawdos API 接口测试")
    print(f"  Target: {BASE_URL}")
    print("=" * 60)
    print()

    tests = [
        # ── Health ──
        ("Health",      test_health),
        ("Env",         test_env),
        # ── Screen ──
        ("Capture PNG", test_screen_capture_png),
        ("Capture JPG", test_screen_capture_jpg),
        # # ── Input ──
        ("Click",       test_input_click),
        ("Move",        test_input_move),
        ("Drag",        test_input_drag),
        ("Keys",        test_input_keys),
        ("Type ASCII",  test_input_type_ascii),
        ("Type 中文",   test_input_type_chinese),
        ("Batch",       test_input_batch),
        # # ── Window ──
        ("Window List", test_window_list),
        ("Window Focus",test_window_focus),
        ("Focus by Proc", test_window_focus_by_process),
        # # ── FileSystem ──
        ("FS Mkdir",    test_fs_mkdir),
        ("FS Write",    test_fs_write),
        ("FS List",     test_fs_list),
        ("FS Read",     test_fs_read),
        ("FS Move",     test_fs_move),
        ("FS Delete",   test_fs_delete),
        # ── Shell ──
        ("Shell Echo",      test_shell_exec_echo),
        ("Shell WorkDir",   test_shell_exec_with_workdir),
        ("Shell MultiArg",  test_shell_exec_multiarg),
        ("Shell Stderr",    test_shell_exec_stderr),
        # ── Negative Tests ──
        ("Auth 401",    test_auth_rejected),
        ("OOB Click",   test_click_out_of_bounds),
        ("Path Escape", test_fs_path_escape),
        ("Focus No Param", test_window_focus_missing_params),
        ("Shell Blocked",   test_shell_blocked_command),
        ("Shell Empty Cmd", test_shell_empty_command),
        ("Shell Dir Escape",test_shell_workdir_escape),
    ]

    passed, failed = 0, 0
    for name, fn in tests:
        try:
            print(f"── {name} {'─' * (50 - len(name))}")
            fn()
            passed += 1
        except Exception as e:
            failed += 1
            print(f"   ❌ FAILED: {e}\n")

    print("=" * 60)
    print(f"  results: {passed} passed / {failed} failed / {passed + failed} total")
    print("=" * 60)


if __name__ == "__main__":
    run_all()