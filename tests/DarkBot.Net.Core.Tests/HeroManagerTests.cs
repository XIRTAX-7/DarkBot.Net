using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Tests.Fakes;

namespace DarkBot.Net.Application.Tests;

public class HeroManagerTests
{
    [Fact]
    public void Tick_reads_hero_from_frida_snapshot()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe
        {
            HeroId = 42,
            HeroHp = 180000,
            HeroMaxHp = 250000,
            HeroX = 193,
            HeroY = 113
        };
        var hero = new HeroManager(addresses, frida, new StarManager());

        addresses.SetScreenManagerAddress(0x1000);
        hero.Tick();

        Assert.True(hero.IsValid);
        Assert.True(hero.HasMapPosition);
        Assert.Equal(42, hero.Id);
        Assert.Equal(180000, hero.Health.Hp);
        Assert.Equal(250000, hero.Health.MaxHp);
        Assert.Equal(193, hero.X);
        Assert.Equal(113, hero.Y);
    }
}
