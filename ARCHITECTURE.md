# DarkBot.Net — Architecture

Clean Architecture: 4 слоя, Unity Frida bridge, WPF UI.

## Слои

```text
DarkBot.Net.Presentation   WPF UI (WPF-UI + ReactiveUI), composition root
        ↓
DarkBot.Net.Application    Bot loop 10 Hz, managers, I*AppService facades
        ↓
DarkBot.Net.Core           Контракты (IGameConnection, IUnityGameBridge), models, Options
        ↑
DarkBot.Net.Infrastructure Unity Frida bridge, credentials, config persistence
```

## Проекты

| Проект | Назначение |
|--------|------------|
| `DarkBot.Net.Core` | `I*Api`, `IGameConnection`, `IUnityGameBridge`, domain models |
| `DarkBot.Net.Application` | `BotLoopService`, managers, AppServices |
| `DarkBot.Net.Infrastructure` | `UnityFridaGameApi`, login, `StubConfigApi` |
| `DarkBot.Net.Presentation` | Views, ViewModels, `Program.cs` |

## AppService-фасады (UI → Application)

- `ILoginAppService` — credentials + Unity WebView autologin
- `IGameLaunchAppService` — attach к `DarkOrbit.exe` + Frida bridge
- `IBotControlAppService` — start/pause/stop бота
- `IMovementAppService` — move ship (async для UI)
- `IGameConnectionStatusAppService` — фаза подключения

## DI (Presentation)

```csharp
services.AddApplication();
services.AddInfrastructure(configuration);
services.AddPresentationUi();
```

## Игровой путь

```text
DarkOrbit.exe (Unity IL2CPP)
  → darkorbit-unity-bridge/agent.js (FridaCLR)
  → Infrastructure.UnityFridaGameApi (IGameConnection + IUnityGameBridge)
  → Application managers (snapshot через RPC, не memory read)
  → BotLoopService @ 10 Hz
  → WPF MapCanvas + Stats
```

C# **не читает память процесса** — только typed snapshot из Frida RPC.

## Bridge contracts

- **Legacy:** `IGameConnection` — Flash pointer API (sync, bot loop only). См. [ADR-002](docs/adr/002-igameconnection-unity.md).
- **Current:** `IUnityGameBridge` — Unity RPC actions (async-first).

## Bot modules

Internal C# classes, без plugin DLL. См. [ADR-001](docs/adr/001-internal-modules.md).

## Удалено / не используется

- Avalonia UI, Electron client, KekkaPlayer, DarkMem, Backpage runtime
- C# Plugins / `AssemblyLoadContext` (Phase 8 — опционально позже)

## Тесты

```powershell
dotnet test DarkBot.Net.slnx -c Release
```

Проекты: `Core.Tests`, `Presentation.Tests`, `Config.Tests`.
