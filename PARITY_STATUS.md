# DarkBot.Net — статус parity (Unity/WPF)

Обновлено: 2026-06-29

**Единственный поддерживаемый путь игры:** `DarkOrbit.exe` (Unity v1.1.103) → FridaCLR + `darkorbit-unity-bridge/agent.js` → .NET через RPC snapshot.

**Отключено:** Electron client, Flash AVM bridge, DarkMem, KekkaPlayer, Backpage runtime, C# Plugins.

---

## Архитектура

```
DarkOrbit.exe (Unity IL2CPP)
    → unity_bridge_agent.js (Frida agent)
    → UnityFridaGameApi (C# Infrastructure)
    → MapManager / HeroManager / EntityManager / StatsManager (Application)
    → BotLoopService @ 10 Hz
    → WPF MapCanvas + StatsPanel
```

C# **не читает память процесса** — только typed snapshot из Frida RPC.

---

## Слои

| Слой | Компонент | Статус |
|------|-----------|--------|
| Client | Unity DarkOrbit.exe | ✅ пользователь запускает вручную |
| Bridge | `darkorbit-unity-bridge` schema v2 | ✅ snapshot + moveTo + select/collect/attack (Ф1) |
| Bot | C# managers from bridge probe | 🟡 ~40% Java parity |
| UI | WPF + SkiaSharp map | ✅ shell, map click move (async) |
| Config | `JsonConfigApi` + persistence | 🟡 Collect UI ✅; бот не читает config (Ф3 tail) |
| Modules | `BotModuleRunner` + `DisconnectModule` | ❌ Collector Phase 2 |

---

## Bridge RPC (Phase 1 — done)

| RPC | Java analog | Статус |
|-----|-------------|--------|
| `getStatus` / entity snapshot | entity list | ✅ |
| `moveTo` | MovementAPI | ✅ |
| `selectEntity` | GameAPI.selectEntity | ✅ |
| `collectTo` | DIRECT_COLLECT_BOX | ✅ |
| `attackLaser` | triggerLaserAttack | ✅ |
| `useItem` | HeroItemsAPI | ❌ stub → Collector (Ф2) |

Контракт C#: `IUnityGameBridge` (ADR-002). Manual smoke: [docs/phase1-smoke-checklist.md](docs/phase1-smoke-checklist.md).

**Тесты:** 225 Vitest (agent) + 109 .NET — все зелёные (2026-06-29).

---

## Миграция Java → .NET

План: [`.cursor/plans/актуализация_миграции_java→.net_2754a1ac.plan.md`](../.cursor/plans/актуализация_миграции_java→.net_2754a1ac.plan.md)

| Milestone | Критерий | Статус |
|-----------|----------|--------|
| M1 Bridge complete | select + collect + attack RPC + tests | ✅ code; manual smoke — checklist |
| M2 Collector works | 10 min автосбор на 1-1 | ❌ Ф2 |
| M3 Config wired | profile save/load влияет на поведение | 🟡 backend + Collect UI |
| M4 Kill & Collect | NPC + boxes одновременно | ❌ Ф4 |

**Следующий критический путь:** Ф2 `CollectorModule` + `IModule`.

---

## Исторический контекст

Старые документы (Avalonia, Electron, DarkMem) — см. архив в `MIGRATION_LOG.md` (секция Archive).
