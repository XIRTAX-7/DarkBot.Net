# DarkBot.Net — Migration Log

Журнал поэтапной миграции DarkBot (Java 11) → .NET 10.  
План: [`.cursor/plans/darkbot_.net_10_+_mcp_migration_fd423926.plan.md`](../.cursor/plans/darkbot_.net_10_+_mcp_migration_fd423926.plan.md)

---

## Статус фаз

| Фаза | Статус | DoD |
|------|--------|-----|
| **0** — Solution skeleton | ✅ Завершена | `dotnet build` + 7 тестов; 8 .NET проектов; 20 типов; DI в Ui; CI |
| **1** — Native Bridge POC | ✅ Завершена | DarkMemAPI + KekkaPlayer shim; heroHP из C#; 19 тестов |
| **2** — Core + Backpage | ✅ Завершена | HeroManager, MapManager, StatsManager, bot-loop 10 Hz, Backpage SID |
| **3** — Avalonia UI | ✅ Завершена | MainWindow, MapCanvas (Skia), ConfigTree, StatsPanel, LoginScreen, verifier sidecar |
| **4** — C# Plugins | ✅ Завершена | DefaultPlugin: SampleModule, AntiPush, PalladiumModule; ALC hot-reload |
| **5** — MCP (опц.) | ⏳ Следующая | DarkBot.Net.Mcp |

---

## История

### 2026-06-13 — Сессия 1 (начало Phase 0)

**Сделано:**
- [x] Установлен `Avalonia.Templates@12.0.4`
- [x] Созданы проекты через `dotnet new classlib` (net10.0):
  - `src/DarkBot.Net.Api`
  - `src/DarkBot.Net.Config`
  - `src/DarkBot.Net.Core`
  - `src/DarkBot.Net.Backpage`
  - `src/DarkBot.Net.Plugins.Abstractions`
  - `src/DarkBot.Net.Agent.Windows`
- [x] Созданы тест-проекты: `tests/DarkBot.Net.*.Tests` (xUnit)
- [x] Пересоздан `src/DarkBot.Net.Ui` через `dotnet new avalonia.app -f net10.0`
- [x] Обновлён `DarkBot.Net.slnx`
- [x] Созданы каталоги `plugins/`, `sidecars/` (пустые)

---

### 2026-06-13 — Сессия 1 (Phase 0 завершена)

**Сделано:**
- [x] `Directory.Build.props` — общие настройки net10.0 / nullable
- [x] Project references между всеми проектами
- [x] Порт первых **20 типов** (см. таблицу ниже) — все ✅
- [x] DI-каркас в Ui:
  - `Microsoft.Extensions.Hosting`
  - `ServiceCollectionExtensions.AddDarkBotUi()` → `IBackpageApi` / `BackpageService`
  - `Program.AppHost` стартует до Avalonia
- [x] `src/DarkBot.Net.Agent.Bridge/` — CMake placeholder (`DarkBotBridge.h`, `MemoryBridge.cpp`)
- [x] GitHub Actions: `.github/workflows/darkbot-net.yml`
- [x] **7 unit-тестов**, все проходят
- [x] `dotnet build DarkBot.Net.slnx -c Release` — OK

**Команды проверки:**
```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
```

**Известные отличия от Java (намеренные, Phase 0):**
- `ILockable.Lock` вместо `getLockType()` + enum `EntityLock` (конфликт имён в C#)
- `IMovementApi.WasMovingIn()` вместо `isMoving(long)` (конфликт с property `IsMoving`)
- `IAttacker.IsAttackingTarget()` вместо `isAttacking(Lockable)`
- `BackpageService` — skeleton; полная логика `BackpageManager` в Phase 2
- `IConfigApi` — только сигнатуры без default-реализаций tree-walk

---

## Структура solution

```
DarkBot.Net/
├── DarkBot.Net.slnx
├── MIGRATION_LOG.md
├── Directory.Build.props
├── src/
│   ├── DarkBot.Net.Api/              ← интерфейсы, BotTimer, BotHttpClient
│   ├── DarkBot.Net.Config/           ← stub (Phase 1+)
│   ├── DarkBot.Net.Core/             ← AddDarkBotCore()
│   ├── DarkBot.Net.Backpage/         ← BackpageService skeleton
│   ├── DarkBot.Net.Plugins.Abstractions/
│   ├── DarkBot.Net.Agent.Bridge/     ← C++ CMake (Phase 1)
│   ├── DarkBot.Net.Agent.Windows/    ← IGameApi
│   └── DarkBot.Net.Ui/               ← entry point + DI host
├── plugins/
├── sidecars/
└── tests/
```

## Первые 20 файлов для порта (Phase 0)

| # | Java | C# | Проект | Статус |
|---|------|----|--------|--------|
| 1 | `Location.java` | `ILocation` + `GameLocation` | Api | ✅ |
| 2 | `Health.java` | `IHealth` | Api | ✅ |
| 3 | `Entity.java` | `IEntity` | Api | ✅ |
| 4 | `Ship.java` | `IShip` | Api | ✅ |
| 5 | `Npc.java` | `INpc` | Api | ✅ |
| 6 | `Box.java` | `IBox` | Api | ✅ |
| 7 | `Portal.java` | `IPortal` | Api | ✅ |
| 8 | `HeroAPI.java` | `IHeroApi` | Api | ✅ |
| 9 | `MovementAPI.java` | `IMovementApi` | Api | ✅ |
| 10 | `EntitiesAPI.java` | `IEntitiesApi` | Api | ✅ |
| 11 | `StatsAPI.java` | `IStatsApi` + `Stats` | Api | ✅ |
| 12 | `BackpageAPI.java` | `IBackpageApi` | Api | ✅ |
| 13 | `ConfigAPI.java` | `IConfigApi` | Api | ✅ |
| 14 | `Feature.java` | `[Feature]` attribute | Plugins.Abstractions | ✅ |
| 15 | `Module.java` | `IModule` | Plugins.Abstractions | ✅ |
| 16 | `Task.java` | `IBotTask` | Plugins.Abstractions | ✅ |
| 17 | `Timer.java` | `BotTimer` | Api | ✅ |
| 18 | `Http.java` | `BotHttpClient` | Api | ✅ |
| 19 | `GameAPI.java` | `IGameApi` | Agent.Windows | ✅ |
| 20 | `BackpageManager.java` | `BackpageService` | Backpage | ✅ |

---

## Заметки

- **JNI / KekkaPlayer**: прямой P/Invoke невозможен — Phase 1 через C++ shim
- **Flash (DarkFlash.ocx)**: Windows-only, BINARY_REUSE
- **Плагины Java JAR**: не совместимы — Phase 4 через `AssemblyLoadContext`
- **Следующий шаг**: Phase 5 — MCP Server (опционально)

---

### 2026-06-14 — Сессия 6 (Phase 4: C# Plugins)

**Сделано:**
- [x] `DarkBot.Net.Plugins` — `PluginLoadContext` (collectible ALC), `FeatureRegistry`, `FeatureActivator`, `PluginHostedService`
- [x] Hot-reload: `FileSystemWatcher` на `plugins/*.dll` + кнопка Reload в UI
- [x] `ModuleController` + `BotApi` — тик активного модуля и enabled behaviors в `BotRuntime`
- [x] API stubs: `MovementApi`, `EntitiesApi`, `RepairApi`, `I18nApi`, `OreApi`, `StarSystemApi`, `LegacyModuleApi`
- [x] `plugins/DarkBot.Net.DefaultPlugin` — `SampleModule`, `AntiPush`, `PalladiumModule`
- [x] `PluginPanelControl` — список features, выбор активного модуля, reload
- [x] `DarkBot.Net.Plugins.Tests` — discovery + SampleModule tick
- [x] **17 unit-тестов** (Core/Api/Config/Backpage/Plugins), build + test OK

**Команды проверки:**
```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
dotnet run --project src/DarkBot.Net.Ui -c Release
```

**Phase 4 — намеренные упрощения (vs Java):**
- VerifierChecker / JAR signing — пропущено (private fork)
- `PalladiumModule` — без полного `LootCollectorModule`; sell-ветка работает, farm — stub
- `MovementApi` — без KekkaPlayer (random move stub)
- `SampleModule` — без ExtraMenus / NpcFlags (Swing-only)
- Plugin distribution — DLL drop в `plugins/`, не NuGet

**Следующий шаг:** Phase 5 — MCP Server (`DarkBot.Net.Mcp`, опционально)

---

### 2026-06-14 — Сессия 2 (Phase 1: DarkMemAPI POC)

**Сделано:**
- [x] C++ shim `DarkBotBridge.dll` — embedded JVM + `eu.darkbot.api.DarkMem` → `DarkMemAPI.dll` (JNI)
- [x] Публичный C API: `bridge_init`, `bridge_read_int/long/double`, `bridge_open_process`, `bridge_get_version`
- [x] `build-bridge.ps1` + CMake (`JniHost.cpp`, `MemoryBridge.cpp`)
- [x] C# P/Invoke (`DarkBotBridgeNative`) + `NativeMemoryBridge` + `GameMemoryReader`
- [x] `ReadHeroHp(shipAddress)` — offset chain как в Java (`Ship+184` → `Health+48` bindable)
- [x] `DarkBot.Net.Agent.Windows.Tests` — **8 тестов** (bridge init + memory helpers)
- [x] CI: шаг `Build native bridge` (Java 11 + CMake)
- [x] `dotnet build` + `dotnet test` — **15 тестов**, все проходят

**Команды проверки:**
```powershell
cd DarkBot.Net\src\DarkBot.Net.Agent.Bridge
.\build-bridge.ps1 -Configuration Release

cd ..\..
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
```

**Артефакты:**
- `lib/DarkBotBridge.dll` — C++ shim (копируется при сборке)
- `lib/DarkMemAPI.dll` — оригинальный JNI bridge (BINARY_REUSE)

**Осталось в Phase 1:**
- [x] KekkaPlayer JNI shim — `bridge_kekka_move_ship`, `bridge_kekka_is_valid`, memory reads
- [x] `NativeGameBridge` — единая точка входа (DarkMem + KekkaPlayer)
- [x] E2E тест `HeroHpE2ETests` (опционально: `DARKBOT_GAME_PID` + `DARKBOT_SHIP_ADDRESS`)

---

### 2026-06-14 — Сессия 3 (Phase 1 завершена: KekkaPlayer)

**Сделано:**
- [x] `KekkaPlayer.java` stub + lazy load в JVM (graceful fallback если DLL недоступен)
- [x] `KekkaShim.cpp` — C API: `bridge_kekka_*` (version, isValid, moveShip, readInt/Long/Double)
- [x] `NativeKekkaBridge` + `NativeGameBridge` (DarkMem attach + KekkaPlayer login)
- [x] Тесты Kekka: availability, isValid, moveShip no-throw
- [x] E2E heroHP: env `DARKBOT_GAME_PID` + `DARKBOT_SHIP_ADDRESS` (skip без игры)
- [x] **19 тестов** total, все проходят

**Следующий шаг:** Phase 3 — Avalonia UI (MainWindow, MapCanvas, ConfigTree)

---

### 2026-06-14 — Сессия 4 (Phase 2: Core + Backpage)

**Сделано:**
- [x] `HeroManager` — `IHeroApi`, чтение HP/id из native memory (`BotAddressRegistry` + `IGameMemoryAccess`)
- [x] `MapManager` — map id / dimensions из `mapAddressStatic+256`, синхронизация `hero.Map`
- [x] `StatsManager` — `IStatsApi` + `ISessionMetadataProvider`, credits/uridium/honor из heroInfo
- [x] `StarManager` — minimal `ById()` (полный graph — Phase 3+)
- [x] `BotLoopService` — `IHostedService`, target **10 Hz** (100 ms), `IBotController`
- [x] `BackpageService` — SID validation loop (`CheckSidValid`, HTTP без redirect)
- [x] `BackpageBackgroundService` — daemon thread 100 ms (port `BackpageManager.run`)
- [x] DI: `AddDarkBotCore()` + `AddDarkBotBackpage()` в Ui host
- [x] **24 unit-тестов**, все проходят (+5 Core, +1 Backpage)

**Команды проверки:**
```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
```

**Phase 2 — намеренные упрощения (vs Java):**
- `HeroManager`: без Drive, keybinds, pet, ship mode selectors (Phase 3+)
- `MapManager`: без EntityList, bounds/zoom, safeties (Phase 3+)
- `StarManager`: только `ById`, без portal graph / Dijkstra
- `StatsManager`: sid/instance из `SetSession` / memory (string read из Flash — Phase 3+)
- `BackpageService`: без Hangar/Auction/Nova managers (Phase 3+)

**Следующий шаг:** Phase 4 — C# Plugins

---

### 2026-06-14 — Сессия 5 (Phase 3: Avalonia UI)

**Сделано:**
- [x] `MainWindow` — toolbar (Start/Pause, Config, Login), status line, backpage indicator
- [x] `MapCanvasControl` — SkiaSharp через `ICustomDrawOperation` (сетка, hero, HP label; клик = start/stop)
- [x] `StatsPanelControl` — credits, uridium, exp, honor, ping, tick stats
- [x] `ConfigWindow` + `ConfigTreeControl` — TreeView + value editor (stub tree)
- [x] `PluginPanelControl` — placeholder для Phase 4
- [x] `LoginWindow` — SID + instance URL + user id → `BackpageService.SetSession`
- [x] `StubConfigApi` в `DarkBot.Net.Config` — минимальное дерево настроек + `AddDarkBotConfig()`
- [x] `BotUiStateService` — snapshot hero/map/stats/backpage для UI (250 ms refresh)
- [x] `VerifierSidecarHostedService` — запуск `verifier.jar` или dev HTTP stub `POST /verify`
- [x] `appsettings.json` — LibPath, VerifierPath, VerifierPort, ProfilesPath
- [x] `sidecars/verifier/start-verifier.ps1`
- [x] **27 unit-тестов**, build + test OK

**Команды проверки:**
```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
dotnet run --project src/DarkBot.Net.Ui -c Release
```

**Phase 3 — намеренные упрощения (vs Java Swing):**
- MapCanvas: без entities/portals/minimap Flash (Phase 4+ / native bridge)
- ConfigTree: stub вместо полного Config tree + Condition DSL
- Login: только SID tab (user/pass + saved logins — позже)
- Stats: без XYChart time series (графики — Phase 4+)
- Verifier: dev stub по умолчанию если `verifier.jar` отсутствует

**Следующий шаг:** Phase 3.5 — game login (user/pass + captcha)

---

### 2026-06-14 — Сессия 6 (Phase 3.5: Game login)

**Сделано:**
- [x] `DarkBot.Net.Login` — `LoginService`, `LoginData`, `LoginHtmlParser` (порт `LoginUtils`)
- [x] HTTP login: frontpage GET → captcha → POST → `dosid` cookie → `findPreloader` (`internalMapRevolution` + flash embed)
- [x] Captcha: `CompositeCaptchaSolver` (manual `g-recaptcha-response` → `dark_backpage --captcha` sidecar)
- [x] `BackpageSidecarLocator` — путь к `dark_backpage` из `appsettings.json`
- [x] `LoginWindow` — TabControl: **User & password** + **SID** (user id из preloader, без ручного ввода)
- [x] `LoginViewModel` — async `LoginCommand`, `ApplySession` → `StatsManager` + `BackpageService`
- [x] `IBackpageApi.SetSession` — публичный контракт для login flow
- [x] **12 login unit-тестов** + **29 total**, build + test OK

**Команды проверки:**
```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
dotnet run --project src/DarkBot.Net.Ui -c Release
```

**Конфиг sidecar (ручная установка DarkBackpage):**
```json
"BackpageSidecarPath": "./sidecars/backpage/dark_backpage.exe",
"BackpageSidecarMinVersion": "1.3.0"
```

**Phase 3.5 — намеренные упрощения (vs Java):**
- Saved logins / `credentials.json` / master password — позже
- Auto-login CLI (`-login login.properties`) — позже
- DarkBackpage auto-download из GitHub releases — позже (сейчас manual path)
- KekkaPlayer / Flash client launch — Agent track

**Следующий шаг:** Phase 4 — C# Plugins (или saved logins)

---

### 2026-06-14 — Сессия 7 (Agent track: KekkaPlayer game launch wiring)

**Сделано:**
- [x] Native bridge launch exports: `setData`, `createWindow`, `setFlashOcxPath`, `setSize`, `setLocalProxy`, `reload`, `queryBytes`, DarkMem process list
- [x] `NativeLibrarySetup` — LibSetup manifest download, Flash OCX path, PATH/JAVA_HOME prep
- [x] `KekkaPlayerGameApi`, `GameLauncherService`, `GameSessionStore`, `FlashVarBuilder`, `KekkaPlayerProxyServer`
- [x] `BotInstallerService` — memory scan → `BotAddressRegistry` (screenManager, heroInfo, settings, connectionManager)
- [x] `ExtraMemoryReader.searchClassClosure`, `GameMemoryAccess` Kekka routing, `MovementApi` → `moveShip`
- [x] UI: login → `LaunchAsync`, auto-launch on existing session, game status line, attach PID picker
- [x] DI lifecycle: `NativeGameBridgeShutdownService`, `GameAutoLaunchService`, `GameReloginService`

**Команды проверки:**
```powershell
cd DarkBot.Net/src/DarkBot.Net.Agent.Bridge
./build-bridge.ps1 -Configuration Release
cd ../..
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
dotnet run --project src/DarkBot.Net.Ui -c Release
```

**Требования для E2E (Flash window):**
- `./lib/KekkaPlayer.dll`, `DarkBotBridge.dll`, `DarkMemAPI.dll`
- `%APPDATA%/DarkBot/lib/DarkFlash.ocx`
- `JAVA_HOME` (64-bit JDK for embedded JVM)

**Следующий шаг:** E2E manual test login → Flash window → bot ticks with real client
