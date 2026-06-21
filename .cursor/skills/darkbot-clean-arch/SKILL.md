---
name: darkbot-clean-arch
description: Clean Architecture для DarkBot.Net — Core / Application / Infrastructure / Presentation, AppService-фасады, Frida-only game path. Используй при создании фич, сервисов, ViewModels, DI.
alwaysApply: false
---

# DarkBot.Net Clean Architecture

## Куда класть код

| Что добавляешь | Слой | Папка |
|----------------|------|-------|
| Интерфейс порта (`IGameConnection`) | Core | `Interfaces/Game/` |
| DTO / snapshot модель | Core | `Models/Game/` |
| Manager / bot tick logic | Application | `Services/Bot/` |
| UI use case фасад | Application | `Contracts/` + `Services/` |
| Frida / HTTP / Process | Infrastructure | `Game/` или `Auth/` |
| Avalonia ViewModel | Presentation | `ViewModels/` |

## AppService (без MediatR)

```csharp
// Contracts/IGameLaunchAppService.cs
public interface IGameLaunchAppService
{
    Task LaunchAndConnectAsync(GameLaunchRequest request, CancellationToken ct = default);
}

// Services/Game/GameLaunchAppService.cs — orchestration only
public sealed class GameLaunchAppService(...) : IGameLaunchAppService { }
```

Регистрация: Scrutor scan `*AppService` в `AddApplication()`.

## DI порядок (Presentation)

```csharp
services.AddApplication();
services.AddInfrastructure(configuration);
services.AddPresentationUi();
```

## Frida-only

Managers читают `IGameFridaProbe`, не `ReadProcessMemory`.
Путь клиента: `DarkBot.Net/Darkorbit-client/` (auto-detect).

## Антипаттерны

- ViewModel → `FridaGameApi` напрямую
- Application → `Process.Start`
- Core → `Microsoft.Extensions.Hosting` реализации
