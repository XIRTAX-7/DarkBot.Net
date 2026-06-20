namespace DarkBot.Net.Agent.Windows.Game;

public enum GameConnectionPhase
{
    NotStarted,
    Launching,
    WaitingForGameLoad,
    Connected,
    Failed
}

public sealed class GameProcessInfo
{
    public required int Pid { get; init; }

    public required string Name { get; init; }
}

public interface IGameConnection
{
    GameApiMode Mode { get; }

    GameConnectionPhase Phase { get; }

    bool IsLaunched { get; }

    bool IsValid { get; }

    string? LastFailureReason { get; }

    int ReadInt(long address);

    long ReadLong(long address);

    double ReadDouble(long address);

    long SearchPattern(ReadOnlySpan<byte> pattern);

    long SearchClassClosure(Func<long, bool> pattern);

    void MoveShip(long screenManager, long x, long y, long collectableAddress = 0);

    void SelectEntity(ReadOnlySpan<int> taggedArgs) { }

    void UseItem(long screenManager, string itemId, int methodIndex, params long[] args) { }

    void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1) { }

    bool InvokeMethod(long objectPtr, int methodIndex, params long[] args) => false;

    void Reload();

    void HandleRefresh(bool useFakeDailyLogin = true);

    long LastInternetReadTime();

    void ClearCache(string pattern);

    IReadOnlyList<GameProcessInfo> GetProcesses();

    void OpenProcess(long pid);

    event Action<GameConnectionPhase>? PhaseChanged;
}
