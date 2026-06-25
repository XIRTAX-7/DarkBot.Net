# DarkBot.Net

DarkBot для DarkOrbit — переписан на .NET 10 / C# 14 с WPF UI (WPF-UI + ReactiveUI) и управлением Unity-клиентом.

## Структура репозитория

| Путь | Описание |
|------|----------|
| [`src/`](src/) | .NET solution — Clean Architecture, 4 слоя |
| [`tests/`](tests/) | xUnit тест-проекты |
| [`darkorbit-unity-bridge/`](darkorbit-unity-bridge/) | Агент для Unity-клиента |
| [`sidecars/`](sidecars/) | Опциональные sidecar (backpage, verifier) |
| [`build/`](build/) | MSBuild targets (сборка и bundling `agent.js`) |

## Архитектура (4 слоя)

```text
DarkBot.Net.Presentation   WPF UI, composition root
        ↓
DarkBot.Net.Application    Bot loop, managers, I*AppService
        ↓
DarkBot.Net.Core           Контракты, модели, Options
        ↑
DarkBot.Net.Infrastructure Unity, Login, Backpage, Config
```

| Слой | Проект | Ответственность |
|------|--------|-----------------|
| **Core** | `DarkBot.Net.Core` | `I*Api`, `IGameConnection`, models — без реализаций |
| **Application** | `DarkBot.Net.Application` | `BotLoopService`, managers, AppService-фасады |
| **Infrastructure** | `DarkBot.Net.Infrastructure` | Unity bridge, login/backpage HTTP, config persistence |
| **Presentation** | `DarkBot.Net.Presentation` | Views, ViewModels, `Program.cs` |

Подробнее: [ARCHITECTURE.md](ARCHITECTURE.md).

### Игровой путь данных

```text
Unity-клиент
  → darkorbit-unity-bridge/agent/dist/agent.js
  → Infrastructure.Game (IGameConnection)
  → Application managers (snapshot через RPC)
  → BotLoopService @ 10 Hz
```

C# **не читает память процесса** — только typed snapshot из Unity bridge.

Статус интеграции: [PARITY_STATUS.md](PARITY_STATUS.md).

## Требования

- **Windows x64**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **Node.js 22+** и `npm` (сборка `darkorbit-unity-bridge/agent`)
- **Unity-клиент** DarkOrbit

```powershell
cd darkorbit-unity-bridge/agent
npm ci
npm run verify
```

## Быстрый старт

1. Собрать и запустить UI:

```powershell
dotnet build DarkBot.Net.slnx
dotnet run --project src/DarkBot.Net.Presentation
```

2. Войти (логин/пароль). Бот запустит Unity-клиент (или подключится к уже запущенному), выполнит авторизацию и дождётся загрузки карты.

3. После входа на карту — запустить бота из главного окна.

> При `dotnet build` Presentation автоматически собирает `agent.js` и копирует его в output рядом с exe.

## Конфигурация

`src/DarkBot.Net.Presentation/appsettings.json` (секция `DarkBot`).

Локальные переопределения — в `appsettings.Local.json` (не коммитится). Переменные окружения с префиксом `DARKBOT_`.

Подробнее: [darkorbit-unity-bridge/docs/INTEGRATION.md](darkorbit-unity-bridge/docs/INTEGRATION.md).

## Тесты

```powershell
dotnet test DarkBot.Net.slnx -c Release
```

## CI

GitHub Actions (`.github/workflows/ci.yml`): verify Unity bridge agent → `dotnet build` → `dotnet test`.
