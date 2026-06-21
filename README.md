# DarkBot.Net

DarkBot for DarkOrbit — .NET 10 rewrite with Avalonia UI, native memory bridge, and Frida-based game control.

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
    → HTTP localhost:44570 (Frida — move, select, collect)
    → WS localhost:44568 (control — reload, pid, window)
    → WS localhost:44569 (packets — invalid session detection)
    → DarkMem native reads (entity lists, hero stats)
    → bot loop @ 10 Hz
```

See [PARITY_STATUS.md](PARITY_STATUS.md) for v1 game-client integration status and [MIGRATION_LOG.md](MIGRATION_LOG.md) for migration history from Java DarkBot.

## Requirements

- **Windows** (game path is Windows-only today)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **Java 11+** (JNI bridge for DarkMem)
- **Node.js 18+** and `npm` (Darkorbit-client)
- **Python 3** with `frida`, `psutil` (game actions sidecar)

```bash
cd Darkorbit-client
npm install
pip install -r darkDev/requirements.txt
```

Native DLLs (`DarkBotBridge.dll`, `DarkMemAPI.dll`, `DarkBot.jar`) are downloaded automatically on first run into `./lib/`.

## Quick start

1. Build and run the UI:

```bash
dotnet build DarkBot.Net.slnx
dotnet run --project src/DarkBot.Net.Ui
```

2. Log in (credentials or SID). The bot will:
   - launch **Darkorbit-client** with your session
   - patch client settings (`Movement`, `NoSandbox`)
   - wait for Pepper Flash PID and Frida HTTP `/status`

3. Wait until the game map loads (`internalMapRevolution`), then verify:

```bash
curl http://127.0.0.1:44570/status
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

Leave `DarkorbitClientPath` empty to auto-detect `Darkorbit-client/` next to the solution root, or set `DARKORBIT_CLIENT_PATH`.

## Client origin

[`Darkorbit-client/`](Darkorbit-client/) is based on [kaiserdj/Darkorbit-client](https://github.com/kaiserdj/Darkorbit-client) with DarkBot-specific Frida integration in `darkDev/`.

## License

GPL-3.0 — see [LICENSE](LICENSE).
