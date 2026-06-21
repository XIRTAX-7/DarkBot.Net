namespace DarkBot.Net.Agent.Windows.Game;

public enum GameConnectionPhase
{
    NotStarted,
    Launching,
    WaitingForGameLoad,
    Connected,
    Failed
}

public interface IGameConnection
{
    GameApiMode Mode { get; }

    GameConnectionPhase Phase { get; }

    bool IsLaunched { get; }

    bool IsValid { get; }

    string? LastFailureReason { get; }

    void MoveShip(long screenManager, long x, long y, long collectableAddress = 0);

    void SelectEntity(ReadOnlySpan<int> taggedArgs);

    void UseItem(long screenManager, string itemId, int methodIndex, params long[] args);

    void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1);

    bool InvokeMethod(long objectPtr, int methodIndex, params long[] args);

    void Reload();

    void HandleRefresh(bool useFakeDailyLogin = true);

    long LastInternetReadTime();

    void ClearCache(string pattern);

    event Action<GameConnectionPhase>? PhaseChanged;
}
