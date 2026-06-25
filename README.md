# DarkBot.Net

DarkBot для DarkOrbit — переписан на .NET 10 / C# 14 с WPF UI (WPF-UI + ReactiveUI) и управлением Unity-клиентом.

## Структура репозитория

| Путь | Описание |
|------|----------|
| [`src/`](src/) | .NET solution — Clean Architecture |
| [`tests/`](tests/) | xUnit тест-проекты |
| [`sidecars/`](sidecars/) | Опциональные sidecar (backpage, verifier) |

## Clean Architecture

```text
DarkBot.Net.Presentation   WPF UI, composition root
        ↓
DarkBot.Net.Application    Bot loop, managers, I*AppService
        ↓
DarkBot.Net.Core           Contracts, models, Options
        ↑
DarkBot.Net.Infrastructure Unity, Login, Backpage, Config
```

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Core** | `DarkBot.Net.Core` | `I*Api`, `IGameConnection`, models — no implementations |
| **Application** | `DarkBot.Net.Application` | `BotLoopService`, managers, AppService facades |
| **Infrastructure** | `DarkBot.Net.Infrastructure` | Unity bridge, login/backpage HTTP, config persistence |
| **Presentation** | `DarkBot.Net.Presentation` | Views, ViewModels, `Program.cs` |

Подробнее: [ARCHITECTURE.md](ARCHITECTURE.md).

### Игровой путь данных

```text
Unity-клиент
  → Infrastructure.Game (IGameConnection)
  → Application managers (snapshot через RPC)
  → BotLoopService @ 10 Hz
```

C# **не читает память процесса** — только typed snapshot из Unity bridge.

Статус интеграции: [PARITY_STATUS.md](PARITY_STATUS.md).

## Требования

- **Windows x64**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **Unity-клиент** DarkOrbit

## Быстрый старт

1. Собрать и запустить UI:

```powershell
dotnet build DarkBot.Net.slnx
dotnet run --project src/DarkBot.Net.Presentation
```

2. Войти (логин/пароль). Бот запустит Unity-клиент (или подключится к уже запущенному), выполнит авторизацию и дождётся загрузки карты.

3. После входа на карту — запустить бота из главного окна.

## Конфигурация

`src/DarkBot.Net.Presentation/appsettings.json` (секция `DarkBot`).

Локальные переопределения — в `appsettings.Local.json` (не коммитится). Переменные окружения с префиксом `DARKBOT_`.

## Тесты

```powershell
dotnet test DarkBot.Net.slnx -c Release
```

## CI

GitHub Actions (`.github/workflows/ci.yml`): `dotnet build` → `dotnet test`.
