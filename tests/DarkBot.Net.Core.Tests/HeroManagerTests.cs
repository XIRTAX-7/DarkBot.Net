using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Game.Items;
using Microsoft.Extensions.Logging.Abstractions;

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
        var hero = new HeroManager(addresses, frida, new StarManager(), NullLogger<HeroManager>.Instance);

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

    [Fact]
    public void SetMode_does_not_overwrite_game_active_configuration()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe
        {
            HeroId = 1,
            HeroHp = 1000,
            HeroMaxHp = 1000,
            HeroConfigId = 1,
        };
        var hero = new HeroManager(addresses, frida, new StarManager(), NullLogger<HeroManager>.Instance);
        addresses.SetScreenManagerAddress(0x1000);
        hero.Tick();

        Assert.Equal(HeroConfiguration.First, hero.ActiveConfiguration);

        var runMode = ShipMode.Of(HeroConfiguration.Second, ISelectableItem.Formation.Standard);
        hero.SetMode(runMode);

        Assert.Equal(HeroConfiguration.First, hero.ActiveConfiguration);
        Assert.False(hero.IsInMode(runMode));
    }
}
