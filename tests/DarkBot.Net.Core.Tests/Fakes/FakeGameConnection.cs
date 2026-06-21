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

    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0) { }
    public void SelectEntity(ReadOnlySpan<int> taggedArgs) { }
    public void UseItem(long screenManager, string itemId, int methodIndex, params long[] args) { }
    public void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1) { }
    public bool InvokeMethod(long objectPtr, int methodIndex, params long[] args) => false;
    public void Reload() { }
    public void HandleRefresh(bool useFakeDailyLogin = true) { }

    public long LastInternetReadTime()
    {
        LastInternetReadTimeCallCount++;
        if (ThrowOnLastInternetReadTime)
            throw new InvalidOperationException("Game connection not ready.");

        return LastInternetReadTimeValue;
    }

    public void ClearCache(string pattern) { }
}
