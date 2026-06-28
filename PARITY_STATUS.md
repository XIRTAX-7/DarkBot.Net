# DarkBot.Net — статус parity (Unity/WPF)

Обновлено: 2026-06-28

**Единственный поддерживаемый путь игры:** `DarkOrbit.exe` (Unity v1.1.102) → FridaCLR + `darkorbit-unity-bridge/agent.js` → .NET через RPC snapshot.

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
| Bridge | `darkorbit-unity-bridge` schema v2 | 🟡 snapshot + moveTo; select/collect/attack — Phase 1 |
| Bot | C# managers from bridge probe | 🟡 ~35% Java parity |
| UI | WPF + SkiaSharp map | ✅ shell, map click move (async) |
| Config | `StubConfigApi` | ❌ MVP Phase 3 |
| Modules | `BotModuleRunner` + `DisconnectModule` | ❌ Collector Phase 2 |

---

## Bridge RPC (Phase 1 blockers)

| RPC | Java analog | Статус |
|-----|-------------|--------|
| `getStatus` / entity snapshot | entity list | ✅ |
| `moveTo` | MovementAPI | ✅ |
| `selectEntity` | GameAPI.selectEntity | ❌ Phase 1 |
| `collectBox` | DIRECT_COLLECT_BOX | ❌ Phase 1 |
| `attack` | triggerLaserAttack | ❌ Phase 1 |

Контракт C#: `IUnityGameBridge` (ADR-002).

---

## Миграция Java → .NET

План: [`.cursor/plans/java→.net_миграция_68f48221.plan.md`](../.cursor/plans/java→.net_миграция_68f48221.plan.md)

| Milestone | Критерий |
|-----------|----------|
| M1 Bridge complete | select + collect + attack RPC + tests |
| M2 Collector works | 10 min автосбор на 1-1 |
| M3 Config wired | profile save/load влияет на поведение |
| M4 Kill & Collect | NPC + boxes одновременно |

---

## Исторический контекст

Старые документы (Avalonia, Electron, DarkMem) — см. архив в `MIGRATION_LOG.md` (секция Archive).
