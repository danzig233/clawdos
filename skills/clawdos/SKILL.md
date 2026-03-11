---
name: clawdos
description: "Windows automation via Clawdos API: screen capture, mouse/keyboard input, window management, file-system operations, and shell command execution. Use when the user wants to control or inspect a Windows host remotely."
metadata: {"openclaw": {"emoji": "🐾", "requires": {"env": ["CLAWDOS_API_KEY", "CLAWDOS_BASE_URL"]}, "primaryEnv": "CLAWDOS_API_KEY"}}
---

## Clawdos Windows Execution Interface

This skill exposes 18 tools that let you operate a Windows machine
through the Clawdos REST API running at `CLAWDOS_BASE_URL`
(default `http://127.0.0.1:17171`).

### Tool Groups

| Group | Tools |
|---|---|
| Health | `health_check`, `get_env` |
| Screen | `screen_capture` |
| Input | `mouse_click`, `mouse_move`, `mouse_drag`, `key_combo`, `type_text`, `input_batch` |
| Window | `window_list`, `window_focus` |
| FileSystem | `fs_list`, `fs_read`, `fs_write`, `fs_mkdir`, `fs_delete`, `fs_move` |
| Shell | `shell_exec` |

### Authentication
All authenticated endpoints require the `X-Api-Key` header matching the
value set in `clawdos-config.json` on the host.

### Security Notes
- `shell_exec` is restricted server-side; only whitelisted commands are
  allowed.
- `fs_*` operations are sandboxed to the `workingDirs` declared in
  Clawdos config; path escapes return 403.