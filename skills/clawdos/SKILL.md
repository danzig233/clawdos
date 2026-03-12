---
name: clawdos
description: "Windows automation via Clawdos API: screen capture, mouse/keyboard input, window management, file-system operations, and shell command execution. Use when the user wants to control or inspect a Windows host remotely."
metadata: {"openclaw": {"emoji": "🐾", "requires": {"env": ["CLAWDOS_BASE_URL", "CLAWDOS_API_KEY"]}, "primaryEnv": "CLAWDOS_API_KEY"}}
---

## Clawdos Windows Execution Interface

This skill exposes 18 tools that let you operate a Windows machine
through the Clawdos REST API.

### Configuration

| Setting | Environment Variable | Default | Description |
|---------|---------------------|---------|-------------|
| `base_url` | `CLAWDOS_BASE_URL` | `http://127.0.0.1:17171` | Windows host service address (e.g., `http://192.168.1.100:17171`) |
| `api_key` | `CLAWDOS_API_KEY` | - | API key (must match `clawdos-config.json` on Windows host) |

### ⚠️ Requirements
**This skill requires a corresponding server running on your Windows host.**
Download and follow the setup instructions here: [danzig233/clawdos](https://github.com/danzig233/clawdos.git)

### Tool Groups

| Group | Tools |
|---|---|
| Health | `health_check`, `get_env` |
| Screen | `screen_capture` |
| Input | `mouse_click`, `mouse_move`, `mouse_drag`, `mouse_scroll`, `key_combo`, `type_text`, `input_batch` |
| Window | `window_list`, `window_focus` |
| FileSystem | `fs_list`, `fs_read`, `fs_write`, `fs_mkdir`, `fs_delete`, `fs_move` |
| Shell | `shell_exec` |

### Authentication
All authenticated endpoints require the `X-Api-Key` header matching the
value set in `clawdos-config.json` on the host.

### Operation Strategy
- **Prefer Keyboard & Shell**: To minimize errors from visual coordinate estimation, prioritize using keyboard shortcuts (`key_combo`, `type_text`) or shell commands (`shell_exec`) over mouse operations whenever possible.
- **Targeted Mouse Usage**: Reserve precise mouse operations (`mouse_click`, `mouse_move`, `mouse_drag`) strictly for necessary UI interactions (e.g., clicking a specific button on a web page, navigating a software interface, or focusing an input field). 
- **Scrolling**: Using `mouse_scroll` is safe and recommended for navigating long pages or documents.

### Visual Feedback Loop & Precision
When precision is required (e.g., clicking small buttons or specific text), follow this **Move-Verify-Correct** loop:
1. **Move**: Use `mouse_move` to the estimated target coordinates.
2. **Verify**: Immediately use `screen_capture` to see where the cursor landed relative to the UI element.
3. **Correct**: If the cursor is offset, calculate the pixel difference and use `mouse_move` again with the corrected coordinates.
4. **Action**: Only call `mouse_click` once you have verified the cursor is correctly positioned.

**Scaling Note**: Always check the resolution in `get_env` against your current screenshot dimensions. If they differ, provide `view_width` and `view_height` to input tools to enable automatic coordinate scaling.

### Security Notes
- `shell_exec` is restricted server-side; only whitelisted commands are
  allowed.
- `fs_*` operations are sandboxed to the `workingDirs` declared in
  Clawdos config; path escapes return 403.