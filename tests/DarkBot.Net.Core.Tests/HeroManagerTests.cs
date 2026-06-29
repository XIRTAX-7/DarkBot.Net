using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game.Items;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public class HeroManagerTests
{
    private static HeroManager CreateHero(
        FakeGameFridaProbe frida,
        FakeGameConnection? bridge = null,
        BotAddressRegistry? addresses = null)
    {
        var registry = addresses ?? new BotAddressRegistry();
        return new HeroManager(
            registry,
            frida,
            bridge ?? new FakeGameConnection(),
            new StarManager(),
            NullLogger<HeroManager>.Instance);
    }

    [Fact]
    public void Tick_reads_hero_from_frida_snapshot_without_screen_manager_address()
    {
        var frida = new FakeGameFridaProbe
        {
            HeroId = 7,
            HeroHp = 50000,
            HeroMaxHp = 80000,
            HeroShield = 12000,
            HeroMaxShield = 20000,
            HeroX = 1000,
            HeroY = 2000,
        };
        var hero = CreateHero(frida);

        hero.Tick();

        Assert.True(hero.IsValid);
        Assert.Equal(50000, hero.Health.Hp);
        Assert.Equal(12000, hero.Health.Shield);
    }

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
        var hero = CreateHero(frida);

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
        var hero = CreateHero(frida);
        addresses.SetScreenManagerAddress(0x1000);
        hero.Tick();

        Assert.Equal(HeroConfiguration.First, hero.ActiveConfiguration);

        var runMode = ShipMode.Of(HeroConfiguration.Second, ISelectableItem.Formation.Standard);
        hero.SetMode(runMode);

        Assert.Equal(HeroConfiguration.First, hero.ActiveConfiguration);
        Assert.False(hero.IsInMode(runMode));
    }

    [Fact]
    public void TriggerLaserAttack_calls_unity_bridge_when_ready()
    {
        var addresses = new BotAddressRegistry();
        addresses.SetScreenManagerAddress(0x1000);
        var bridge = new FakeGameConnection();
        var hero = CreateHero(
            new FakeGameFridaProbe { HeroId = 1, HeroHp = 1, HeroMaxHp = 1 },
            bridge,
            addresses);

        Assert.True(hero.TriggerLaserAttack());
        Assert.Equal(1, bridge.AttackCalls);
    }

    [Fact]
    public void SetAttackMode_selects_target_via_bridge()
    {
        var addresses = new BotAddressRegistry();
        addresses.SetScreenManagerAddress(0x1000);
        var bridge = new FakeGameConnection();
        var hero = CreateHero(
            new FakeGameFridaProbe { HeroId = 1, HeroHp = 1, HeroMaxHp = 1 },
            bridge,
            addresses);
        var npc = new NpcEntity(bridge)
        {
            Id = 99,
            Location = new MutableLocationInfo(),
        };
        npc.Location.Update(500, 600);

        Assert.True(hero.SetAttackMode(npc));
        Assert.Contains((99, 500, 600), bridge.SelectEntityCalls);
    }
}
