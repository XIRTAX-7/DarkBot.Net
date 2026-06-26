# Промпт: независимое code review — Clean Architecture (DarkBot.Net)

Скопируй этот промпт в **новый чат** без доступа к истории рефакторинга. Приложи diff ветки или укажи коммит.

---

## Роль

Ты — senior .NET architect. Проведи **независимое** ревью рефакторинга DarkBot.Net на Clean Architecture. Не предполагай, что автор прав. Ищи нарушения границ слоёв, утечки зависимостей и регрессии.

## Контекст проекта

Desktop-бот DarkOrbit: **WPF** (`DarkBot.Net.Presentation`) + **Application** (bot loop, managers, `I*AppService`) + **Infrastructure** (Frida, Unity client, auth) + **Core** (контракты, модели).

Целевая схема зависимостей:

```text
Presentation → Application (только I*AppService + UI-типы)
Application → Core
Infrastructure → Application, Core
Core → (ничего)
```

Composition root: `DarkBot.Net.Presentation.Extensions.DependencyInjection.BuildDarkBotHost`.

## Что проверить

### 1. Границы слоёв

- [ ] ViewModels зависят **только** от `I*AppService` и UI-контрактов (`IConfigWindowService`, `IShellWindowService`), не от `Infrastructure.*`, `HeroManager`, `IMovementApi`, `GameSessionStore`.
- [ ] `DarkBot.Net.Application` не ссылается на Infrastructure, WPF, Frida, Process, HttpListener.
- [ ] `DarkBot.Net.Infrastructure` не ссылается на Presentation.
- [ ] Hosted services: `VerifierSidecarHostedService` в Infrastructure; `GameAutoLaunchHostedService` в Application; UI-only wiring в Presentation.

### 2. AppService-фасады

Проверь наличие и корректность:

| Контракт | Назначение |
|----------|------------|
| `IBotControlAppService` | start/pause/stop |
| `IBotStatusAppService` | snapshot бота для UI |
| `IGameConnectionStatusAppService` | фаза подключения (без UiStrings) |
| `IGameLaunchAppService` | запуск клиента |
| `IGameClientRestartAppService` | рестарт клиента |
| `ILoginAppService` | login + credentials + session |
| `IAppShellAppService` | login vs main при старте |
| `IMovementAppService` | move ship |
| `IGameShutdownAppService` | shutdown при закрытии окна |

Реализации `*AppService` в Application или Infrastructure — но **интерфейсы** только в `Application/Contracts`.

### 3. Структура Presentation

```text
Presentation/
├── Formatting/      # UiStrings, форматирование статусов
├── Ui/              # окна, навигация (Config, Shell)
├── Diagnostics/     # UI-диагностика
├── ViewModels/
├── Views/
└── Controls/        # MapCanvas: render settings + Skia drawers
```

- [ ] Нет папки `Services/` с бизнес-логикой.
- [ ] Нет `Presentation/Models/` — VM биндит `Application.DTOs.Responses.*`.
- [ ] `GameConnectionStatusFormatter` — единственное место UiStrings для статуса игры.

### 4. Application: public vs BotEngine

```text
PUBLIC:  Contracts/, DTOs/, Mappers/, Services/*AppService
INTERNAL: BotEngine/ (Loop, Install, Runtime, Modules, Managers, State, Addresses, Statistics)
```

- [ ] Нет корневых `Bot/`, `Managers/`, `Entities/`, `Memory/` в Application.
- [ ] Presentation не ссылается на `BotEngine.*`.

### 5. Read models

- Application: `DTOs/Responses/Bot/*` — единственный read model для UI.
- Application: `Mappers/Bot/*` — маппинг managers → DTO.
- `MapJavaStationFallbackTests` → `Application.Mappers.Bot`.

### 6. DI и Scrutor

- `AddAppServices()` сканирует `*AppService` в Application.
- Infrastructure регистрирует `IGameClientRestartAppService`, `IGameShutdownAppService` вручную.
- Нет дублирующей регистрации hosted services.

### 7. Тесты

- `MapJavaStationFallbackTests` → Application.Mappers.Bot.
- `GameAutoLaunchHostedServiceTests` → Application.
- `VerifierSidecarHostedServiceTests` → Infrastructure.

### 8. Регрессии

- Login flow (credentials → session → launch).
- Auto-launch при сохранённой сессии.
- Main screen refresh (bot status, map canvas).
- Shell shutdown (stop game on window close).

## Формат ответа

1. **Вердикт** — соответствует CA / частично / не соответствует.
2. **Критические нарушения** — файл, строка, что не так, как исправить.
3. **Рекомендации** — улучшения без блокера.
4. **Пропущенные тесты** — что добавить.
5. **Чеклист** — таблица пунктов выше (pass/fail).

Не предлагай переписать всё с нуля. Давай конкретные правки по файлам.
