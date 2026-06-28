using DarkBot.Net.Core.Interfaces.Bot;
using DarkBot.Net.Application.Services.Bot;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Core.Tests;

public sealed class BotDiagnosticsAppServiceTests
{
    [Fact]
    public void Get_ReadsBotLoopMetricsWithoutSideEffects()
    {
        var bot = new StubBotController { LastTickMs = 50 };
        var stats = new StubStatsApi { Ping = 42, MemoryMb = 128 };
        var sut = new BotDiagnosticsAppService(bot, stats);

        var snapshot = sut.Get();

        Assert.Equal(50, snapshot.LastTickMs);
        Assert.Equal(128, snapshot.MemoryMb);
        Assert.Equal(42, snapshot.Ping);
        Assert.Equal(10, snapshot.LoopHz);
        Assert.Equal(0, bot.TickSideEffects);
    }

    private sealed class StubBotController : IBotController
    {
        public int TickSideEffects { get; private set; }

        public bool IsRunning => true;

        public long TickCount => 1;

        public double LastTickMs { get; init; }

        public double LastLoopPeriodMs => LastTickMs > 0 ? LastTickMs + 50 : 0;

        public void Start() => TickSideEffects++;

        public void Pause() => TickSideEffects++;

        public void Stop() => TickSideEffects++;
    }

    private sealed class StubStatsApi : IStatsApi
    {
        public int Ping { get; init; }

        public double MemoryMb { get; init; }

        public IStatsApi.IStat GetStat(IStatsApi.IStatKey key) => throw new NotSupportedException();

        public double GetStatValue(IStatsApi.IStatKey key) =>
            key.Name switch
            {
                nameof(Core.Game.Stats.Stats.Bot.Memory) => MemoryMb,
                _ => 0
            };

        public IStatsApi.IStat RegisterStat(IStatsApi.IStatKey key) => throw new NotSupportedException();

        public void SetStatValue(IStatsApi.IStatKey key, double newValue) => throw new NotSupportedException();

        public void ResetStats() => throw new NotSupportedException();
    }
}
