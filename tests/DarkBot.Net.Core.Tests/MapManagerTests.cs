using DarkBot.Net.Core.Game;
using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public class MapManagerTests
{
    private static MapManager CreateMap(
        BotAddressRegistry addresses,
        StarManager star,
        HeroManager hero,
        IGameFridaProbe frida) =>
        new(addresses, frida, star, hero, NullLogger<MapManager>.Instance);

    private static HeroManager CreateHero(BotAddressRegistry addresses, StarManager star, IGameFridaProbe frida) =>
        new(addresses, frida, star, NullLogger<HeroManager>.Instance);

    [Fact]
    public void Tick_switches_map_when_frida_snapshot_available()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe { MapId = 16 };
        var star = new StarManager();
        var hero = CreateHero(addresses, star, frida);
        var map = CreateMap(addresses, star, hero, frida);

        addresses.SetScreenManagerAddress(0x1000);
        map.Tick();

        Assert.Equal(16, map.MapId);
        Assert.Equal("4-4", hero.Map.Name);
    }

    [Fact]
    public void Tick_keeps_loading_until_frida_map_id_is_valid()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe { MapId = 0 };
        var star = new StarManager();
        var hero = CreateHero(addresses, star, frida);
        var map = CreateMap(addresses, star, hero, frida);

        addresses.SetScreenManagerAddress(0x1000);
        map.Tick();

        Assert.Equal(-1, map.MapId);
        Assert.Equal("Загрузка", hero.Map.Name);

        frida.MapId = 9;
        map.Tick();

        Assert.Equal(9, map.MapId);
        Assert.Equal("3-1", hero.Map.Name);
    }

    [Fact]
    public void Tick_applies_frida_map_every_tick_without_pointer_change()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe { MapId = 0 };
        var star = new StarManager();
        var hero = CreateHero(addresses, star, frida);
        var map = CreateMap(addresses, star, hero, frida);

        addresses.SetScreenManagerAddress(0x1000);
        map.Tick();
        Assert.Equal(-1, map.MapId);

        frida.MapId = 9;
        map.Tick();

        Assert.Equal(9, map.MapId);
        Assert.Equal("3-1", hero.Map.Name);
    }

    [Fact]
    public void Tick_loads_static_portals_when_map_switches()
    {
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe { MapId = 16 };
        var star = new StarManager();
        var hero = CreateHero(addresses, star, frida);
        var map = CreateMap(addresses, star, hero, frida);

        addresses.SetScreenManagerAddress(0x1000);
        map.Tick();

        Assert.Equal(16, map.MapId);
        Assert.Equal(6, map.Portals.Count);
        Assert.Contains(map.Portals, p => p.TargetShortName == "1-5" && p.X == 7000 && p.Y == 13500);
    }
}
