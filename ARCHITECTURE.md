# DarkBot.Net — Architecture

Clean Architecture (по образцу Sample/Desktop): 4 слоя, Frida-only game path, без plugin DLL.

## Слои

```text
DarkBot.Net.Presentation   Avalonia UI, composition root
        ↓
DarkBot.Net.Application    Bot loop, managers, I*AppService
        ↓
DarkBot.Net.Core           Контракты, модели, Options (без реализаций)
        ↑
DarkBot.Net.Infrastructure Frida, Electron, Login, Backpage, Config
```

## Проекты

| Проект | Было | Назначение |
|--------|------|------------|
| `DarkBot.Net.Core` | `DarkBot.Net.Api` + entities | `I*Api`, `IGameConnection`, models |
| `DarkBot.Net.Application` | `DarkBot.Net.Core` | `BotLoopService`, managers, AppServices |
| `DarkBot.Net.Infrastructure` | Agent + Login + Backpage + Config | Реализации портов |
| `DarkBot.Net.Presentation` | `DarkBot.Net.Ui` | Views, ViewModels, `Program.cs` |

## AppService-фасады (UI → Application)

- `ILoginAppService` — логин credentials/SID + apply session
- `IGameLaunchAppService` — запуск Darkorbit-client + Frida connect
- `IBotControlAppService` — start/pause/stop бота

## DI (Presentation)

```csharp
services.AddApplication();
services.AddInfrastructure(configuration);
services.AddPresentationUi();
```

## Игровой путь

```text
DarkBot.Net/Darkorbit-client
  → avm_bridge (:44570)
  → Infrastructure.Game (FridaGameApi)
  → Core.IGameFridaProbe
  → Application managers
  → BotLoopService @ 10 Hz
```

## Удалено

- `DarkBot.Net.Plugins` / `Plugins.Abstractions` / `plugins/`
- Plugin UI и `ModuleController` registry

Встроенный `BotModuleRunner` + `DisconnectModule` остаются для internal pause logic.
