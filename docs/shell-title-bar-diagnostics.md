# Title Bar Diagnostics — поток данных

Метрики в правой части `ui:TitleBar` (TICK · RAM · PING · FPS) показывают оперативное состояние бота.

## Схема

```text
BotLoopService (10 Hz)
    └─ StatsManager.TickAverageStats(tickMs) → Memory, TickTime averages
    └─ IBotController.LastTickMs

IBotDiagnosticsAppService.Get()   ← read-only, без side effects
    └─ BotDiagnosticsSnapshot (Tick, RAM, Ping, LoopHz)

TitleBarDiagnosticsUiCoordinator (DispatcherTimer 250 ms, UI thread)
    └─ TitleBarDiagnosticsViewModel.Apply(snapshot)
           └─ TitleBarDiagnosticsFormatter → строки для XAML

ShellWindowView.TitleBar.TrailingContent
    └─ TitleBarDiagnosticsControl → TitleBarDiagnosticMetricControl × 4
```

## Почему не `IBotStatusAppService.Capture()`

`Capture()` для Main/карты — **тяжёлый** снимок:

- `frida.Refresh()` → Frida RPC (`GetStatusJsonAsync().GetAwaiter().GetResult()`)
- `hero.Tick()`, `map.Tick()`

Вызов с UI-потока каждые 250 ms (в т.ч. на Login и во время bootstrap игры) блокировал UI и мешал загрузке клиента DarkOrbit.

Title bar использует только **`IBotDiagnosticsAppService`** — читает уже накопленные значения из bot loop и stats.

## Слои

| Слой | Ответственность |
|------|-----------------|
| **Application** | `BotDiagnosticsSnapshot`, `BotDiagnosticsAppService`, сбор RAM в `StatsManager` |
| **Presentation / Formatting** | `TitleBarDiagnosticsFormatter` |
| **Presentation / ViewModels** | `TitleBarDiagnosticsViewModel` |
| **Presentation / Ui/Shell** | `TitleBarDiagnosticsUiCoordinator` — только `Get()`, не `Capture()` |
| **Presentation / Controls/Shell** | XAML pill-метрики |

## Метрики

| UI | Источник | Примечание |
|----|----------|------------|
| TICK | `LastTickMs` | Длительность последнего bot tick, мс |
| RAM | `MemoryMb` | Working set процесса бота |
| PING | `Ping` | `—`, пока stats = 0 |
| FPS | `LoopHz` | 1000 / LastTickMs, частота bot loop |

## DI

```csharp
// Application — авто-регистрация *AppService
services.AddApplication();

// Presentation
services.AddSingleton<TitleBarDiagnosticsViewModel>();
services.AddSingleton<TitleBarDiagnosticsUiCoordinator>();
```

## Биндинг в XAML

```xml
<shellControls:TitleBarDiagnosticsControl DataContext="{Binding TitleBarDiagnostics}" />
```

`ReactiveUserControl.ViewModel` не выставляет `DataContext` для дочерних биндингов — нужен явный `DataContext` или синхронизация в code-behind.
