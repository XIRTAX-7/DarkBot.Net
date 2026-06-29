namespace DarkBot.Net.Core.Interfaces.Game;

/// <summary>
/// Unity Frida bridge actions без Flash pointer API.
/// P0 RPC (move/select/collect/attack) — Phase 1; контракт зафиксирован в Phase 0 (ADR-002).
/// </summary>
public interface IUnityGameBridge
{
    /// <summary>Движение к точке на карте.</summary>
    Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default);

    /// <summary>Выбор entity на карте.</summary>
    Task<bool> SelectEntityAsync(int entityId, int mapX, int mapY, CancellationToken cancellationToken = default);

    /// <summary>Сбор box через MoveHeroToCoordinates + collectable Unit.</summary>
    Task<bool> CollectBoxAsync(int entityId, int mapX, int mapY, CancellationToken cancellationToken = default);

    /// <summary>Атака laser (keybind / AttackLaserRunCommand).</summary>
    Task<bool> AttackAsync(CancellationToken cancellationToken = default);

    /// <summary>Использование предмета — stub до Collector + collect.auto_cloak (Phase 2).</summary>
    Task<bool> UseItemAsync(string itemId, CancellationToken cancellationToken = default);
}
