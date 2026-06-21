# DarkBot.Net

DarkBot for DarkOrbit — .NET 10 rewrite with Avalonia UI and **Frida-only** game control (no DarkMem / JNI).

This repository is a **monorepo** with two main parts:

| Path | Description |
|------|-------------|
| [`src/`](src/) | .NET solution — bot core, UI, login, backpage, Windows agent |
| [`tests/`](tests/) | xUnit test projects |
| [`Darkorbit-client/`](Darkorbit-client/) | Electron game client (Pepper Flash) + Frida sidecar (`darkDev/`) |
| [`plugins/`](plugins/) | Drop-in C# plugin assemblies |
| [`sidecars/`](sidecars/) | Optional sidecar executables (backpage, captcha, etc.) |

## Architecture

```
DarkBot.Net.Ui (Avalonia)
    → login / backpage
    → spawns Darkorbit-client (--dosid)
    → WS localhost:44570/ws (Frida — status push: map, hero, entities, stats)
    → HTTP localhost:44570 (Frida — move, select, collect)
    → WS localhost:44568 (control — reload, pid, window)
    → WS localhost:44569 (packets — invalid session detection)
    → C# managers consume Frida snapshot (no external memory reads)
    → bot loop @ 10 Hz
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
dotnet run --project src/DarkBot.Net.Ui
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

`src/DarkBot.Net.Ui/appsettings.json`:

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
