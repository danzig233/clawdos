# Clawdos

Windows Execution Interface for AI Agents

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows_x64-0078D6?logo=windows&logoColor=white)]()
[![License](https://img.shields.io/badge/License-MIT-green?logo=opensourceinitiative&logoColor=white)]()
[![API](https://img.shields.io/badge/API-19_Endpoints-FF6F00?logo=fastapi&logoColor=white)]()

Clawdos acts as the physical execution interface for AI agents in Windows environments. It exposes a secure, lightweight local HTTP API and CLI wrapper for screen capture, input automation, window control, sandboxed file operations, and controlled shell execution.

[Quick Start](#quick-start) | [API Reference](#api-reference) | [Configuration](#configuration) | [Use Cases](#use-cases)

---

## Overview

Clawdos serves as a secure bridge between an AI Agent (running locally, on Linux, in Docker, or in the Cloud) and a target Windows desktop environment. It wraps system-level Win32 operations in an authenticated .NET 8 Minimal API.

> **Note for AI Agents**: All system capabilities listed below are fully exposed via the unified client wrapper. Instead of writing raw HTTP or parsing Base64 manually, you should interact with these capabilities via the pre-bundled tool package located under `@skills/clawdos/` (by invoking the CLI script `skills/clawdos/scripts/clawdos.py` with standard action actions).

| Capability | Scope of Functionality |
|------------|------------------------|
| **Screen** | Desktop screenshots (optimized via DXGI Desktop Duplication with a GDI fallback) |
| **Input** | Mouse click, move, drag, scroll, key combos, and robust Unicode/IME-safe text typing |
| **Window** | Active window identification, listing visible windows, and focusing |
| **FileSystem** | Directory listing, file reading, and writing locked securely within sandboxed roots |
| **Shell** | Execution of whitelisted console processes with strict timeouts and stream size limits |
| **Health & Env** | Resource utilization, service uptime, screen metrics, DPI, and IME status checks |

---

## Security and Sandboxing

Clawdos is designed from the ground up to prevent arbitrary or malicious actions on the host Windows machine:

- **Authentication**: Mandatory validation via `X-Api-Key` headers on all non-health routes.
- **File Sandbox**: Strict path resolution with traversal protection (`../` prevention); file actions are locked within registered physical `workingDirs`.
- **Shell Safeguards**: Hard timeouts (maximum 120s), strict stdout/stderr buffering limit (maximum 1MB per stream), and an optional command whitelist to prevent arbitrary command execution.
- **Network Boundaries**: Configurable listen IP and port (defaults to `127.0.0.1` to prevent unauthorized external exposure).

---

## Quick Start

### 1. Build
Publish the binary for Windows x64:
```bash
cd src/Clawdos
dotnet publish -c Release -r win-x64 --self-contained
```

### 2. Configure
Edit `clawdos-config.json` in your publish folder:
```json
{
  "clientId": "w10-01",
  "listenIp": "127.0.0.1",
  "port": 17171,
  "apiKey": "your-secret-key-here",
  "workingDirs": ["C:\\ClawdosSandbox"],
  "shellAllowList": null
}
```
*Note: A secure built-in default whitelist is utilized when `shellAllowList` is set to `null`. Key parameters can be overridden using environment variables: `CLAWDOS_LISTEN_IP`, `CLAWDOS_PORT`, and `CLAWDOS_API_KEY`.*

### 3. Run (System Tray Mode)
Launch `Clawdos.exe`. By default, it runs as a desktop application visible in the Windows system tray.
Right-click the tray icon to:
- **Start Service**: Spawns and starts the Kestrel HTTP API host.
- **Stop Service**: Gracefully shuts down the HTTP host.
- **About**: Opens the GitHub project repository.
- **Exit**: Stops the host and terminates the application.

### 4. Run as Windows Service (Optional)
To deploy Clawdos as a headless background daemon instead of a tray application:
```powershell
.\install\Install-ClawdosService.ps1 install
.\install\Install-ClawdosService.ps1 start
```

### 5. Verify Installation
```bash
curl http://127.0.0.1:17171/v1/health
```
Response:
```json
{
  "ok": true,
  "version": "clawdos-0.1.0"
}
```

### 6. Interact via Skills (Recommended)
Instead of writing raw HTTP network requests or manually parsing Base64 strings, you can interact with all Clawdos API capabilities via the pre-bundled skills located under `skills/clawdos/` (or referenced as `@skills/clawdos` in the agent environment).

The Python CLI wrapper script `skills/clawdos/scripts/clawdos.py` exposes a simplified interface to seamlessly trigger clicks, keystrokes, screen capture, file transfers, and shell commands.

**Example: Check Health & Environment**
```bash
export CLAWDOS_BASE_URL="http://127.0.0.1:17171"
export CLAWDOS_API_KEY="your-secret-key-here"

# Execute health action via the skill CLI
python3 skills/clawdos/scripts/clawdos.py health

# Get environment (resolution, DPI scale, active window, IME)
python3 skills/clawdos/scripts/clawdos.py get_env
```

**Example: Screen Capture**
```bash
python3 skills/clawdos/scripts/clawdos.py screen_capture --out /tmp/screen.png --args '{"format": "png"}'
```

**Example: Input & Keyboard Controls**
```bash
python3 skills/clawdos/scripts/clawdos.py click --args '{"x": 500, "y": 500, "button": "left"}'
python3 skills/clawdos/scripts/clawdos.py type_text --args '{"text": "Hello World", "useClipboard": true}'
```

For more details on the skill arguments and a complete list of actions, refer to the [Agent Integration and CLI Client](#agent-integration-and-cli-client) section or read the specialized skill guide at `skills/clawdos/SKILL.md`.

---

## API Reference

All requests must include the `X-Api-Key` header (except `/v1/health`).

| Module | Method | Endpoint | Description |
|--------|--------|----------|-------------|
| Health | `GET` | `/v1/health` | Service availability and usage stats |
| Env | `GET` | `/v1/env` | Gets resolution, DPI scale, taskbar, active window, and IME status |
| Screen | `GET` | `/v1/screen/capture` | Capture current frame (PNG or JPEG with quality) |
| Input | `POST` | `/v1/input/click` | Simulated mouse click (Left, Right, Middle) |
| Input | `POST` | `/v1/input/move` | Smooth or instant mouse cursor move |
| Input | `POST` | `/v1/input/drag` | Drag cursor from source to destination coordinate |
| Input | `POST` | `/v1/input/scroll` | Simulate mouse scroll wheel |
| Input | `POST` | `/v1/input/keys` | Inject virtual key combinations (e.g., Ctrl+S) |
| Input | `POST` | `/v1/input/type` | Type text (supports IME bypass or Clipboard paste fallback) |
| Input | `POST` | `/v1/input/batch` | Execute sequential action array with fail-fast options |
| Window | `GET` | `/v1/window/list` | List names, handles, and process IDs of visible windows |
| Window | `POST` | `/v1/window/focus` | Bring target window to foreground |
| FileSystem | `GET` | `/v1/fs/list` | List directory contents with metadata |
| FileSystem | `GET` | `/v1/fs/read` | Read sandboxed file (returns Base64 string) |
| FileSystem | `POST` | `/v1/fs/write` | Write binary file to the sandbox |
| FileSystem | `POST` | `/v1/fs/mkdir` | Create subdirectory within target sandbox root |
| FileSystem | `POST` | `/v1/fs/delete` | Delete sandboxed file or directory (supports recursive) |
| FileSystem | `POST` | `/v1/fs/move` | Move or rename file/folder with overwrite options |
| Shell | `POST` | `/v1/shell/exec` | Run validated console commands under strict timeouts |

---

## Directory Structure

```
Clawdos/
├── Clawdos.sln
├── README.md
├── LICENSE.txt
├── src/Clawdos/
│   ├── Program.cs            # Entry point, Kestrel initialization, pipeline and routing
│   ├── clawdos-config.json   # Base application configuration
│   ├── Configuration/        # Strong-typed configuration classes
│   ├── Middleware/           # API Key authentication and performance metrics
│   ├── Services/             # Execution and business logic engines
│   ├── Endpoints/            # Route mapping and request parsing
│   ├── Models/               # Structured data schemas (DTOs)
│   └── Native/               # Native Windows API (P/Invoke) declarations
├── skills/                   # Integrated tool package and CLI client
│   └── clawdos/
│       ├── SKILL.md          # Tool descriptions and integration schemas
│       └── scripts/
│           └── clawdos.py    # Python CLI utility mapping API routes
├── install/                  # PowerShell helper scripts for service deployment
└── api/                      # Verification scripts
```

---

## Agent Integration and CLI Client

To make Clawdos instantly consumable by any AI Agent (such as those powered by LangChain, crewAI, AutoGPT, or custom LLM frameworks with shell execution capabilities), Clawdos distributes a pre-bundled Python CLI helper utility at `skills/clawdos/scripts/clawdos.py`.

### Why Use the CLI Client?
While agents can make raw HTTP requests, the `clawdos.py` CLI client is highly recommended for agents because:
- **Zero Boilerplate**: The agent doesn't need to write repetitive network request or error handling code.
- **Robust Binary Transfers**: Simplifies complex operations (e.g., uploading files or capturing screenshots to local disks) without nesting long Base64 strings in JSON payloads.
- **Environment Management**: Automatically parses `CLAWDOS_BASE_URL` and `CLAWDOS_API_KEY` environment variables.

### Agent Guidelines (Standard Integration Loop)

Any LLM-based agent operating a Windows machine via this interface should adhere to the following guidelines:

#### 1. Execution Loop: Observe → Plan → Act → Verify
To maintain coordinate integrity and prevent run-away UI states:
- **Observe**: Call `get_env` or `screen_capture` to ascertain current desktop dimensions, active window title, and active IME.
- **Plan**: Align target interactive points (buttons, input fields) based on physical coordinates.
- **Act**: Emit precise clicks, scrolls, keyboard shortcuts, or text.
- **Verify**: **Always take a follow-up screenshot** (using the `captureAfterMs` parameter) to confirm the action was successful.

#### 2. CLI Invocation Syntax
For agents executing bash commands, invoke actions using:
```bash
python3 skills/clawdos/scripts/clawdos.py <action> --args '<arguments_json>' [--out <output_file>] [--file <input_file>]
```
**Examples:**
- **Capture Screen**:
  ```bash
  python3 skills/clawdos/scripts/clawdos.py screen_capture --out /tmp/screen.png --args '{"format": "png"}'
  ```
- **Type Text (Unicode/Emoji Safe)**:
  ```bash
  python3 skills/clawdos/scripts/clawdos.py type_text --args '{"text": "Clawdos 🦀", "useClipboard": true}'
  ```
- **Run Shell Command**:
  ```bash
  python3 skills/clawdos/scripts/clawdos.py shell_exec --args '{"command": "ipconfig", "args": ["/all"]}'
  ```

#### 3. Agent Best Practices
- **Prefer Keyboard over Mouse**: Desktop layouts can shift. Always prioritize using keyboard hotkeys (`keys`) or shell execution (`shell_exec`) over coordinate clicks when navigating.
- **Non-ASCII Text**: For plain ASCII, use `useClipboard = false` (forces US keyboard layout to bypass active IMEs). For Chinese, Japanese, emojis, or Unicode symbols, **always** set `useClipboard = true` to paste cleanly via the Win32 clipboard.
- **Mutex Integrity**: Clipboard operations use the global `Global\ClawdosClipboardMutex` mutex on Windows. Allow a short pause (e.g., `wait` action or Sleep) after pasting to ensure the target UI captures the clipboard contents before restoring the original values.
- **Directory Paths**: The FileSystem sandbox blocks path traversal. Use relative pathing from the root (e.g., `/hello.txt` is resolved within the root ID 0 sandbox path).

---

## Operational Guidelines

- **Environment Consistency**: Real-time screen capture and input injection are highly sensitive to desktop changes. Maintain a fixed resolution, set a static DPI scale (e.g., 100%), and avoid logging out of active desktop sessions during automation.
- **Coordinate Integrity**: Verify coordinate alignment with the target system metrics before sending large automation sequences.

---

## Use Cases

### 1. Remote Windows Host Control
Deploy Clawdos on a remote physical workstation or VM on your local network. An AI Agent running in the cloud or a Linux server can remotely orchestrate Windows desktop tasks—such as executing legacy ERP tasks, interacting with localized office tools, or collecting visual desktop diagnostics—securely over HTTP.

### 2. Multi-Windows Host Coordination (Agent Swarms)
Run Clawdos on multiple workstations within an enterprise subnet, each with a unique `clientId`. A central orchestrator agent (or an agent swarm) can partition parallel tasks, coordinate automated testing on different OS versions simultaneously, distribute headless data-mining operations, or synchronize multi-device automation workflows.

### 3. Cross-Platform Desktop Bridging
Provide Linux-native AI Agents with a physical "hand" into Windows environments. This allows standard containerized agents to interact with proprietary Windows GUI software (like CAD, desktop editors, and local databases) seamlessly.

### 4. Continuous Integration & Software Automation
Perform automated regression testing, regression validation, or continuous integration verification directly on physical Windows systems. Agents can capture real-time visual frames and execute complex batch action arrays with fail-fast recovery.
