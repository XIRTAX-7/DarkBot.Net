# DarkBot.Net

DarkBot for DarkOrbit — .NET 10 rewrite with Avalonia UI and **Frida-only** game control (no DarkMem / JNI).

This repository is a **monorepo** with two main parts:

| Path | Description |
|------|-------------|
| [`src/`](src/) | .NET solution — 4-layer Clean Architecture |
| [`tests/`](tests/) | xUnit test projects |
| [`Darkorbit-client/`](Darkorbit-client/) | Electron game client (Pepper Flash) + Frida sidecar (`darkDev/`) |
| [`sidecars/`](sidecars/) | Optional sidecar executables (backpage, captcha, etc.) |

## Architecture (4 layers)

```text
DarkBot.Net.Presentation   Avalonia UI, composition root
        ↓
DarkBot.Net.Application    Bot loop, managers, I*AppService
        ↓
DarkBot.Net.Core           Contracts, models, Options
        ↑
DarkBot.Net.Infrastructure Frida, Electron, Login, Backpage, Config
```

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Core** | `DarkBot.Net.Core` | `I*Api`, `IGameConnection`, models — no implementations |
| **Application** | `DarkBot.Net.Application` | `BotLoopService`, managers, AppService facades |
| **Infrastructure** | `DarkBot.Net.Infrastructure` | Frida, login/backpage HTTP, config persistence |
| **Presentation** | `DarkBot.Net.Presentation` | Views, ViewModels, `Program.cs` |

See [ARCHITECTURE.md](ARCHITECTURE.md) for layer rules and migration notes (old project names → new).

### Game data path

```text
DarkBot.Net/Darkorbit-client
  → avm_bridge (WS :44570/ws, HTTP :44570)
  → Infrastructure.Game (FridaGameApi)
  → Application managers (snapshot, not process memory)
  → BotLoopService @ 10 Hz
```

Legacy DarkMem / KekkaPlayer native code is archived locally under `archive/darkmem-native/` (gitignored).

See [PARITY_STATUS.md](PARITY_STATUS.md) for v1 game-client integration status.

## Requirements

- **Windows** (game path is Windows-only today)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **Node.js 18+** and `npm` (Darkorbit-client)
- **Python 3** with `frida`, `psutil`, `aiohttp` (Frida bridge sidecar)

```bash
cd Darkorbit-client
npm install
pip install -r darkDev/requirements.txt
```

## Quick start

1. Build and run the UI:

```bash
dotnet build DarkBot.Net.slnx
dotnet run --project src/DarkBot.Net.Presentation
```

2. Log in (credentials or SID). The bot will:
   - launch **Darkorbit-client** with your session
   - patch client settings (`Movement`, `NoSandbox`)
   - wait for Pepper Flash PID and Frida WS `/status`

3. Wait until the game map loads (`internalMapRevolution`), then verify:

```bash
curl http://127.0.0.1:44570/status
# schemaVersion 2 — mapId, entities[], credits, heroHp
# Status also pushed on ws://127.0.0.1:44570/ws
```

## Configuration

`src/DarkBot.Net.Presentation/appsettings.json`:

```json
{
  "DarkBot": {
    "BrowserApi": "FridaClient",
    "FridaApiPort": 44570,
    "ControlPort": 44568,
    "PacketPort": 44569,
    "EnablePacketBridge": true,
    "DarkorbitClientPath": ""
  }
}
```
