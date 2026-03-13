<div align="center">

# 🦀 Clawdos

**Windows Execution Interface for OpenClaw**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows_x64-0078D6?logo=windows&logoColor=white)]()
[![License](https://img.shields.io/badge/License-MIT-green?logo=opensourceinitiative&logoColor=white)]()
[![API](https://img.shields.io/badge/API-19_Endpoints-FF6F00?logo=fastapi&logoColor=white)]()

> **OpenClaw thinks. Clawdos acts.** A secure local HTTP API for screen capture, input automation, window control, sandboxed file operations, and controlled command execution on Windows.

[Quick Start](#-quick-start) · [API Reference](#-api-reference) · [Configuration](#️-configuration) · [Use Cases](#-use-cases)

</div>

---

## 💡 At a Glance

OpenClaw is the brain, Clawdos is the hand reaching into Windows.

`.NET 8 Minimal API` → `Windows Service` → **19 Endpoints** → **6 Capability Domains**

| Capability | What It Does |
|------|--------|
| 🖥️ **Screen** | Desktop screenshot (DXGI preferred, GDI fallback) |
| 🖱️ **Input** | Mouse · Keyboard · Chinese input · Batch actions |
| 🪟 **Window** | Window enumeration and focus |
| 📂 **FileSystem** | File read/write within sandbox |
| ⚡ **Shell** | Whitelisted command execution |
| 💓 **Health / Env** | Health check · Runtime metrics · Desktop environment detection |

---

## 🤔 Not Another Remote Control Tool?

**Boundaries.** Clawdos exposes only the capabilities that an Agent truly needs, no more, no less.

- `X-Api-Key` endpoint authentication
- File operations locked within `workingDirs` sandbox
- Shell command whitelist + working directory validation + timeout + output length limit
- Configurable listen address/port to prevent accidental exposure

---

## 🚀 Quick Start

### 1️⃣ Build

```

cd src/Clawdos

dotnet publish -c Release -r win-x64 --self-contained

```

### 2️⃣ Configure

Edit `clawdos-config.json` in the publish directory:

```
{
"clientId": "w10-01",
"listenIp": "0.0.0.0",
"port": 17171,
"apiKey": "your-secret-key-here",
"workingDirs": ["C:\OpenClawWorking"],
"shellAllowList": null
}

```

> 💡 Built-in default whitelist is used when `shellAllowList` is `null`.
> Environment variable overrides supported: `CLAWDOS_LISTEN_IP` / `CLAWDOS_PORT` / `CLAWDOS_API_KEY`

### 3️⃣ Run & System Tray

You can run `Clawdos.exe` directly. It will start as a system tray application featuring a crab (🦀) icon.
Right-click the tray icon to access:
- **Start Service**: Starts the HTTP API service.
- **Stop Service**: Stops the HTTP API service.
- **About**: Opens the project's GitHub page.
- **Exit**: Stops the service and closes the application.

### 4️⃣ Install as Service (Optional)

If you prefer to run it as a background Windows Service instead of a tray application:

```
.\install\Install-ClawdosService.ps1 install
.\install\Install-ClawdosService.ps1 start

```

### 5️⃣ Test

```

curl http://127.0.0.1:17171/v1/health

```

You should see `"ok": true` 🎉

---

## 📡 API Reference

| Module | Method | Endpoint | Description |
|------|------|------|------|
| 💓 Health | `GET` | `/v1/health` | Health check (no authentication) |
| 🌍 Env | `GET` | `/v1/env` | Resolution · DPI · Taskbar · Active window · IME |
| 🖥️ Screen | `GET` | `/v1/screen/capture` | Desktop screenshot (PNG / JPEG) |
| 🖱️ Input | `POST` | `/v1/input/click` | Mouse click |
| 🖱️ Input | `POST` | `/v1/input/move` | Mouse move |
| 🖱️ Input | `POST` | `/v1/input/drag` | Mouse drag |
| 🖱️ Input | `POST` | `/v1/input/scroll` | Mouse scroll |
| ⌨️ Input | `POST` | `/v1/input/keys` | Key combinations |
| ⌨️ Input | `POST` | `/v1/input/type` | Text input |
| ⚙️ Input | `POST` | `/v1/input/batch` | Batch actions (fail-fast) |
| 🪟 Window | `GET` | `/v1/window/list` | List visible windows |
| 🪟 Window | `POST` | `/v1/window/focus` | Focus window by title/process name |
| 📂 FS | `GET` | `/v1/fs/list` | List directory |
| 📂 FS | `GET` | `/v1/fs/read` | Read file (base64) |
| 📂 FS | `POST` | `/v1/fs/write` | Write file |
| 📂 FS | `POST` | `/v1/fs/mkdir` | Create directory |
| 📂 FS | `POST` | `/v1/fs/delete` | Delete |
| 📂 FS | `POST` | `/v1/fs/move` | Move/rename |
| ⚡ Shell | `POST` | `/v1/shell/exec` | Execute controlled command |

---

## 📁 Directory Structure

```
Clawdos/
├── Clawdos.sln
├── README.md
├── LICENSE.txt
├── src/Clawdos/
│   ├── Program.cs            # 🚪 Entry: Config · DI · Kestrel · Routing
│   ├── clawdos-config.json   # ⚙️ Default config
│   ├── Configuration/        # 📋 Config models
│   ├── Middleware/            # 🛡️ Auth · Metrics
│   ├── Services/             # 🧠 Core logic
│   ├── Endpoints/            # 🔌 API routes
│   ├── Models/               # 📝 Request/response models
│   └── Native/               # 🔧 P/Invoke declarations
├── skills/                   # 🐾 OpenClaw skill package
│   └── clawdos/
│       ├── SKILL.md          # Skill manifest & documentation
│       └── scripts/
│           └── clawdos.py    # CLI wrapper for API
├── install/                  # 📦 Service installation scripts
└── api/                      # 🧪 API tests
```

---

## 🐾 OpenClaw Skill Package

Clawdos includes a ready-to-use **OpenClaw skill package** (`skills/clawdos/`) that wraps all API endpoints as OpenClaw tools, enabling seamless integration with OpenClaw agents.

### Features

- **Integrated Tools**: Direct mapping of Clawdos endpoints to OpenClaw tools
- **Type Safety**: Full parameter validation and type hints
- **Error Handling**: Robust error handling with meaningful messages
- **Image Support**: Screen capture returns images for visual analysis

### Installation

The easiest way to install the Clawdos skill is via **Clawhub**:

```bash
clawhub install clawdos
```

Alternatively, you can install it manually:

1. Ensure OpenClaw is installed and configured
2. Copy the `skills/clawdos/` directory to your OpenClaw skills directory
3. Install dependencies: `pip install requests>=2.28.0`
4. Configure the skill in your OpenClaw manifest or environment:

```json
{
  "skills": {
    "clawdos": {
      "base_url": "http://127.0.0.1:17171",
      "api_key": "your-secret-key-here"
    }
  }
}
```

### Available Tools

| Category | Tools |
|----------|-------|
| Health | `health_check`, `get_env` |
| Screen | `screen_capture` |
| Input | `mouse_click`, `mouse_move`, `mouse_drag`, `mouse_scroll`, `key_combo`, `type_text`, `input_batch` |
| Window | `window_list`, `window_focus` |
| FileSystem | `fs_list`, `fs_read`, `fs_write`, `fs_mkdir`, `fs_delete`, `fs_move` |
| Shell | `shell_exec` |

Each tool includes detailed descriptions and parameter schemas for optimal agent usage.

## ⚠️ Runtime Tips

> Screen capture and input injection depend on a stable desktop environment.
> Recommended: **fixed resolution, fixed DPI, fixed taskbar position**. Avoid switching user sessions during execution.
> When pixel coordinates are reliable, the upper-layer Agent is reliable.

---

## 🎯 Use Cases

- 🐧 Enable Windows GUI operations for Linux-side Agents
- 🧪 Desktop software automation testing / regression verification
- 📁 Process files and artifacts within sandbox directories
- 🔗 Serve as a lightweight execution node integrated into larger Agent orchestration systems

