using DarkBot.Net.Core.Game.Stats;
using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Tests.Fakes;

namespace DarkBot.Net.Application.Tests;

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
