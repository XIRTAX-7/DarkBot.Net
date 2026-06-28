namespace DarkBot.Net.Core.Interfaces.Game;

/// <summary>
/// Unity Frida bridge actions без Flash pointer API.
/// Реализация RPC — Phase 1; контракт зафиксирован в Phase 0 (ADR-002).
/// </summary>
public interface IUnityGameBridge
{
    /// <summary>Движение к точке на карте.</summary>
    Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default);

    /// <summary>Выбор entity на карте (Phase 1).</summary>
    Task<bool> SelectEntityAsync(int entityId, int mapX, int mapY, CancellationToken cancellationToken = default);

    /// <summary>Сбор box (Phase 1).</summary>
    Task<bool> CollectBoxAsync(CancellationToken cancellationToken = default);

    /// <summary>Атака laser (Phase 1).</summary>
    Task<bool> AttackAsync(CancellationToken cancellationToken = default);

    /// <summary>Использование предмета (Phase 1).</summary>
    Task<bool> UseItemAsync(string itemId, CancellationToken cancellationToken = default);
}
