# ADR-001: Internal C# modules (no plugin DLL)

**Status:** Accepted (Phase 0, 2026-06-28)

## Context

Java DarkBot использует JAR plugins (`FeatureRegistry`, `DefaultPlugin`, community plugins). В .NET ранее была попытка Phase 4 с `AssemblyLoadContext` — откатили из-за нестабильного module API.

## Decision

1. **Bot modules — internal C# classes** в `DarkBot.Net.Application/BotEngine/Modules/`.
2. **`BotModuleRunner`** — единственный registry; без hot-reload DLL.
3. **Plugins (ALC, DmPlugin, KEKW)** — Phase 8, только после стабилизации `IModule` + bridge actions + Config MVP.

## Consequences

- Модули (`CollectorModule`, `LootModule`, …) мержатся в основной репозиторий.
- Нет community plugin API до Phase 8.
- Тестирование модулей — unit tests с `FakeGameConnection` / `IUnityGameBridge` fakes.

## References

- `BotModuleRunner.cs`
- Plan: `.cursor/plans/java→.net_миграция_68f48221.plan.md` Phase 2 / Phase 8
