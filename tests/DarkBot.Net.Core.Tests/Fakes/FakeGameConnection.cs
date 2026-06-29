using DarkBot.Net.Core.Interfaces.Game;

namespace DarkBot.Net.Application.Tests.Fakes;

public sealed class FakeGameConnection : IGameConnection, IUnityGameBridge
{
    public GameConnectionPhase Phase { get; set; } = GameConnectionPhase.NotStarted;
    public bool IsLaunched { get; set; }
    public bool IsValid { get; set; }
    public string? LastFailureReason { get; set; }
    public bool ThrowOnLastInternetReadTime { get; set; }
    public int LastInternetReadTimeCallCount { get; private set; }
    public long LastInternetReadTimeValue { get; set; }

    public List<(int X, int Y)> MoveToCalls { get; } = [];
    public List<(int EntityId, int MapX, int MapY)> SelectEntityCalls { get; } = [];
    public List<(int EntityId, int MapX, int MapY)> CollectBoxCalls { get; } = [];
    public int AttackCalls { get; private set; }

    public event Action<GameConnectionPhase>? PhaseChanged;

    public event Action? BridgeDisconnected;

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

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        MoveToCalls.Add((x, y));
        return Task.CompletedTask;
    }

    public Task<bool> SelectEntityAsync(int entityId, int mapX, int mapY, CancellationToken cancellationToken = default)
    {
        SelectEntityCalls.Add((entityId, mapX, mapY));
        return Task.FromResult(true);
    }

    public Task<bool> CollectBoxAsync(int entityId, int mapX, int mapY, CancellationToken cancellationToken = default)
    {
        CollectBoxCalls.Add((entityId, mapX, mapY));
        return Task.FromResult(true);
    }

    public Task<bool> AttackAsync(CancellationToken cancellationToken = default)
    {
        AttackCalls++;
        return Task.FromResult(true);
    }

    public Task<bool> UseItemAsync(string itemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
