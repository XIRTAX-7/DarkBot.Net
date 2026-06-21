using DarkBot.Net.Api.Game.Stats;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;
using DarkBot.Net.Core.Tests.Fakes;

namespace DarkBot.Net.Core.Tests;

public class StatsManagerTests
{
    [Fact]
    public void Tick_tracks_credits_from_frida_snapshot()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe
        {
            HeroId = 42,
            Credits = 1000,
            IsReady = true
        };
        var stats = new StatsManager(addresses, frida);

        stats.Tick();

        Assert.Equal(1000, stats.GetStat(Stats.General.Credits).Current);
        Assert.Equal(42, stats.UserId);
    }
}
