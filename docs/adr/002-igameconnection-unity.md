# ADR-002: IUnityGameBridge vs legacy IGameConnection

**Status:** Accepted (Phase 0, 2026-06-28)

## Context

`IGameConnection` унаследован от Flash AVM bridge:

- `MoveShip(screenManager, x, y, collectableAddress)`
- `SelectEntity(ReadOnlySpan<int> taggedArgs)` — 8 Flash-tagged args
- `UseItem(screenManager, itemId, methodIndex, pointer args…)`
- `Refine(refineUtilAddress, …)`

Unity path (`darkorbit-unity-bridge`) работает через **RPC snapshot**, без pointer registry. `BotAddressRegistry` + fake `screenManager=1` — compatibility shim.

## Decision

1. **Новый контракт:** `IUnityGameBridge` в `DarkBot.Net.Core/Interfaces/Game/`.
2. **Async-first** для UI и будущих module actions.
3. **Legacy `IGameConnection`** — sync wrappers для bot loop (10 Hz background) до полной миграции в Phase 1.
4. **`GameDirectApi`** — deprecated path; Phase 1 заменит на calls через `IUnityGameBridge`.

### IUnityGameBridge (Phase 0 sketch, Phase 1 RPC)

| Method | Java analog | Phase |
|--------|-------------|-------|
| `MoveToAsync(x, y)` | `MovementAPI.move` | ✅ RPC exists |
| `SelectEntityAsync(id, mapX, mapY)` | `GameAPI.selectEntity` | Phase 1 |
| `CollectBoxAsync()` | `DIRECT_COLLECT_BOX` | Phase 1 |
| `AttackAsync()` | `HeroManager.triggerLaserAttack` | Phase 1 |
| `UseItemAsync(itemId)` | `HeroItemsAPI` | Phase 1 |

## Consequences

- UI map click → `IMovementAppService.MoveToAsync` → `IUnityGameBridge` (no `.GetResult()` on UI thread).
- Bot loop → sync `IGameConnection.MoveShip` → delegates to `MoveToAsync().GetResult()` (background thread OK).
- Phase 1: implement RPC in agent + wire `IUnityGameBridge`; remove fake addresses from `TryGetInstallerAddresses`.

## References

- `IUnityGameBridge.cs`, `IGameConnection.cs`
- `UnityFridaGameApi.cs`
- `GameDirectApi.cs` (legacy)
