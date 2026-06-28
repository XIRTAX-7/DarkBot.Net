namespace DarkBot.Net.Core.Interfaces.Game;

public enum GameConnectionPhase
{
    NotStarted,
    Launching,
    WaitingForGameLoad,
    Connected,
    Failed
}

/// <summary>
/// Порт управления Unity-клиентом через Frida bridge.
/// Flash pointer API (MoveShip/SelectEntity/…) — legacy; новый код использует <see cref="IUnityGameBridge"/>.
/// </summary>
public interface IGameConnection
{
    GameConnectionPhase Phase { get; }

    bool IsLaunched { get; }

    bool IsValid { get; }

    string? LastFailureReason { get; }

    /// <summary>Legacy Flash AVM — только bot loop sync. UI: <see cref="IUnityGameBridge.MoveToAsync"/>.</summary>
    void MoveShip(long screenManager, long x, long y, long collectableAddress = 0);

    /// <summary>Legacy Flash AVM — Phase 1: <see cref="IUnityGameBridge.SelectEntityAsync"/>.</summary>
    void SelectEntity(ReadOnlySpan<int> taggedArgs);

    /// <summary>Legacy Flash AVM — Phase 1: <see cref="IUnityGameBridge.UseItemAsync"/>.</summary>
    void UseItem(long screenManager, string itemId, int methodIndex, params long[] args);

    /// <summary>Legacy Flash refine — не используется в Unity path.</summary>
    void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1);

    bool InvokeMethod(long objectPtr, int methodIndex, params long[] args);

    void Reload();

    void HandleRefresh(bool useFakeDailyLogin = true);

    long LastInternetReadTime();

    void ClearCache(string pattern);

    event Action<GameConnectionPhase>? PhaseChanged;

    /// <summary>Frida bridge WS отключился (push-событие от клиента через bridge).</summary>
    event Action? BridgeDisconnected;
}
