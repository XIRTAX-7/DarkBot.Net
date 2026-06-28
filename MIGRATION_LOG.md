# DarkBot.Net — Migration Log

Журнал миграции DarkBot (Java 11) → DarkBot.Net (.NET 10 / WPF / Unity).

**Актуальный план:** [`.cursor/plans/java→.net_миграция_68f48221.plan.md`](../.cursor/plans/java→.net_миграция_68f48221.plan.md)

---

## Текущий runtime (2026-06-28)

| Компонент | Стек |
|-----------|------|
| UI | WPF (WPF-UI + ReactiveUI) + SkiaSharp map |
| Game client | Unity `DarkOrbit.exe` v1.1.102 |
| Bridge | FridaCLR + `darkorbit-unity-bridge/agent.js` |
| Bot loop | `BotLoopService` @ 10 Hz |
| Architecture | Clean Architecture (4 projects) |

---

## Статус фаз (Java parity plan)

| Фаза | Статус | DoD |
|------|--------|-----|
| **0** — Hygiene + code review | ✅ Завершена | docs sync, orphan tests, EntityInfoStub, IUnityGameBridge ADR, async UI move |
| **1** — Bridge actions | ⏳ Следующая | selectEntity + collectBox + attack RPC |
| **2** — CollectorModule MVP | ⏳ | 10 min автосбор |
| **3** — Config MVP | ⏳ | JSON profiles, UI binding |
| **4** — Loot + Kill & Collect | ⏳ | NpcAttacker, typed entities |
| **5** — Navigation | ⏳ | PortalJumper, StarManager travel |
| **6** — Safety + Repair | ⏳ | SafetyFinder, DisconnectModule |
| **7** — Backpage (опц.) | ⏳ | по потребности |
| **8** — Plugins (опц.) | ⏳ | после stable module API |

---

## Phase 0 — Hygiene (2026-06-28)

**Сделано:**
- [x] `ARCHITECTURE.md`, `PARITY_STATUS.md` синхронизированы с README (Unity/WPF)
- [x] ADR: `docs/adr/001-internal-modules.md`, `002-igameconnection-unity.md`
- [x] `IUnityGameBridge` — новый Unity RPC контракт в Core
- [x] `EntityInfoStub` — одна копия в `Core.Entities`
- [x] Orphan tests: удалены Backpage/Login/Agent.Windows/Api; тесты перенесены в Core.Tests
- [x] `IMovementAppService.MoveToAsync` — UI map click без sync RPC deadlock
- [x] Stub honesty: `HeroManager` combat stubs логируют; `ShipStub` помечен tech debt

**Команды проверки:**
```powershell
cd DarkBot.Net
dotnet build DarkBot.Net.slnx -c Release
dotnet test DarkBot.Net.slnx -c Release --no-build
```

---

## Solution structure

```
DarkBot.Net/
├── DarkBot.Net.slnx
├── src/
│   ├── DarkBot.Net.Core/
│   ├── DarkBot.Net.Application/
│   ├── DarkBot.Net.Infrastructure/
│   └── DarkBot.Net.Presentation/
├── tests/
│   ├── DarkBot.Net.Core.Tests/
│   ├── DarkBot.Net.Presentation.Tests/
│   └── DarkBot.Net.Config.Tests/
├── darkorbit-unity-bridge/
└── docs/adr/
```

---

## Archive (устаревшие фазы — не актуально)

<details>
<summary>Исторические фазы 2026-06-13 (Avalonia, KekkaPlayer, DarkMem, Plugins)</summary>

Ранний план описывал:
- Avalonia UI (`DarkBot.Net.Ui`)
- KekkaPlayer + DarkMem native bridge
- Backpage SID login
- C# Plugins Phase 4 (DefaultPlugin, ALC hot-reload)
- MCP Server Phase 5

Весь этот стек **заменён** на Unity client + WPF + Frida bridge. Исходный журнал сессий сохранён в git history.

</details>
