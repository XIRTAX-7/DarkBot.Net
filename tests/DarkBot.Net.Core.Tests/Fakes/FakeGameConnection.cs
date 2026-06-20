using DarkBot.Net.Agent.Windows.Game;

namespace DarkBot.Net.Core.Tests.Fakes;

public sealed class FakeGameConnection : IGameConnection
{
    public GameApiMode Mode { get; set; } = GameApiMode.FridaClient;
    public GameConnectionPhase Phase { get; set; } = GameConnectionPhase.NotStarted;
    public bool IsLaunched { get; set; }
    public bool IsValid { get; set; }
    public string? LastFailureReason { get; set; }
    public bool ThrowOnLastInternetReadTime { get; set; }
    public int LastInternetReadTimeCallCount { get; private set; }
    public long LastInternetReadTimeValue { get; set; }

    public event Action<GameConnectionPhase>? PhaseChanged;

    public int ReadInt(long address) => 0;
    public long ReadLong(long address) => 0;
    public double ReadDouble(long address) => 0;
    public long SearchPattern(ReadOnlySpan<byte> pattern) => 0;
    public long SearchClassClosure(Func<long, bool> pattern) => 0;
    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0) { }
    public void Reload() { }
    public void HandleRefresh(bool useFakeDailyLogin = true) { }
    public long LastInternetReadTime()
    {
        LastInternetReadTimeCallCount++;
        if (ThrowOnLastInternetReadTime)
            throw new InvalidOperationException("Native bridge is not initialized.");

        return LastInternetReadTimeValue;
    }

    public void ClearCache(string pattern) { }
    public IReadOnlyList<GameProcessInfo> GetProcesses() => Array.Empty<GameProcessInfo>();
    public void OpenProcess(long pid) { }
}
