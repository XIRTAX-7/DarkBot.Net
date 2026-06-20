using DarkBot.Net.Api.Game.Stats;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;
using DarkBot.Net.Core.Tests.Fakes;

namespace DarkBot.Net.Core.Tests;

public class StatsManagerTests
{
    [Fact]
    public void Tick_tracks_credits_from_heroInfo()
    {
        var addresses = new BotAddressRegistry();
        var memory = new FakeGameMemoryAccess();
        var stats = new StatsManager(addresses, memory);

        const long heroInfo = 0x5000;
        addresses.SetHeroInfoAddress(heroInfo);
        memory.SetDouble(heroInfo + 0x178, 1000);
        memory.SetDouble(heroInfo + 0x180, 50);

        stats.Tick();

        Assert.Equal(1000, stats.GetStat(Stats.General.Credits).Current);
        Assert.Equal(50, stats.GetStat(Stats.General.Uridium).Current);
    }
}
