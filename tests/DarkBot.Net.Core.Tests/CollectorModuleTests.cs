using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Application.BotEngine.Runtime;
using DarkBot.Net.Application.BotEngine.Safety;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public sealed class CollectorModuleTests
{
    private sealed class TestBoxInfo : IBoxInfo
    {
        public bool ShouldCollect { get; set; } = true;
        public int WaitTime { get; set; }
        public int Priority { get; set; }
    }

    private static (
        CollectorModule Module,
        FakeGameConnection Bridge,
        ModuleContext Context) CreateHarness(
        FakeGameConnection bridge,
        double heroX,
        double heroY,
        BoxEntity box,
        int heroMapId = 26,
        int? configWorkingMap = null)
    {
        var workingMap = configWorkingMap ?? heroMapId;
        var config = FakeConfigApi.WithCollectorDefaults(workingMap);
        var addresses = new BotAddressRegistry();
        addresses.SetScreenManagerAddress(1);

        var frida = new FakeGameFridaProbe
        {
            MapId = heroMapId,
            HeroId = 1,
            HeroX = heroX,
            HeroY = heroY,
            HeroHp = 50_000,
            HeroMaxHp = 50_000,
        };

        var star = new StarManager();
        var hero = new HeroManager(addresses, frida, bridge, star, NullLogger<HeroManager>.Instance);
        var map = new MapManager(addresses, frida, star, hero, NullLogger<MapManager>.Instance);
        var entitiesApi = new EntitiesApi();
        var entities = new EntityManager(addresses, map, frida, bridge, entitiesApi, config);
        var stats = new StatsManager(addresses, frida);
        var movement = new MovementApi(addresses, bridge, map, hero, NullLogger<MovementApi>.Instance);

        hero.Tick();
        map.Tick();
        entitiesApi.ReplaceSnapshot([], [], [box], [], []);
        stats.Tick(true);

        var context = new ModuleContext(hero, movement, entitiesApi, stats, config, map);
        var safety = new SafetyFinder(context);
        var module = new CollectorModule(context, safety);

        return (module, bridge, context);
    }

    [Fact]
    public void RebuildFromSnapshot_resolves_bonus_box_label_to_config_entry()
    {
        var config = FakeConfigApi.WithCollectorDefaults();
        var addresses = new BotAddressRegistry();
        var frida = new FakeGameFridaProbe
        {
            MapId = 26,
            MapPointer = 0x5000,
            IsReady = true,
            Entities =
            [
                new FridaEntitySnapshot(42, 1100, 1000, "box", Label: "bonus_box"),
            ],
        };
        var bridge = new FakeGameConnection();
        var map = new MapManager(addresses, frida, new StarManager(), new HeroManager(addresses, frida, bridge, new StarManager(), NullLogger<HeroManager>.Instance), NullLogger<MapManager>.Instance);
        var entitiesApi = new EntitiesApi();
        var entityManager = new EntityManager(addresses, map, frida, bridge, entitiesApi, config);

        map.Tick();
        entityManager.Tick();

        var box = entitiesApi.Boxes.Single();
        Assert.Equal("BONUS_BOX", box.TypeName);
        Assert.True(box.Info.ShouldCollect);
    }

    private static BoxEntity CreateBox(FakeGameConnection bridge, double x, double y, bool shouldCollect = true)
    {
        var box = new BoxEntity(bridge, new TestBoxInfo { ShouldCollect = shouldCollect, Priority = 1 })
        {
            Id = 42,
            Location = new MutableLocationInfo(),
            TypeName = "BONUS_BOX",
            Hash = "BONUS_BOX",
        };
        box.Location.Update(x, y);
        return box;
    }

    [Fact]
    public void OnTickModule_moves_toward_box_when_out_of_collect_range()
    {
        var bridge = new FakeGameConnection();
        var box = CreateBox(bridge, 1300, 1000);
        var (module, gameBridge, _) = CreateHarness(bridge, 1000, 1000, box);

        module.OnTickModule();

        Assert.Single(gameBridge.MoveToCalls);
        Assert.Empty(gameBridge.CollectBoxCalls);
    }

    [Fact]
    public void OnTickModule_collects_box_when_in_range()
    {
        var bridge = new FakeGameConnection();
        var box = CreateBox(bridge, 1100, 1000);
        var (module, gameBridge, context) = CreateHarness(bridge, 1000, 1000, box);

        Assert.Equal(1000, context.Hero.X, 1);
        Assert.True(context.Hero.DistanceTo(box) < 250);

        module.OnTickModule();

        Assert.Contains((42, 1100, 1000), gameBridge.CollectBoxCalls);
    }

    [Fact]
    public void OnTickModule_skips_when_not_on_working_map()
    {
        var bridge = new FakeGameConnection();
        var box = CreateBox(bridge, 1100, 1000);
        var (module, gameBridge, _) = CreateHarness(bridge, 1000, 1000, box, heroMapId: 26, configWorkingMap: 1);

        module.OnTickModule();

        Assert.Empty(gameBridge.MoveToCalls);
        Assert.Empty(gameBridge.CollectBoxCalls);
    }

    [Fact]
    public void OnTickModule_skips_disabled_box_type()
    {
        var bridge = new FakeGameConnection();
        var box = CreateBox(bridge, 1100, 1000, shouldCollect: false);
        var (module, gameBridge, _) = CreateHarness(bridge, 1000, 1000, box);

        module.OnTickModule();

        Assert.Empty(gameBridge.MoveToCalls);
        Assert.Empty(gameBridge.CollectBoxCalls);
    }

    [Fact]
    public void BotModuleRunner_ticks_collector_when_running()
    {
        var bridge = new FakeGameConnection();
        var config = FakeConfigApi.WithCollectorDefaults();
        var addresses = new BotAddressRegistry();
        addresses.SetScreenManagerAddress(1);

        var frida = new FakeGameFridaProbe
        {
            MapId = 26,
            HeroId = 1,
            HeroX = 1000,
            HeroY = 1000,
            HeroHp = 50_000,
            HeroMaxHp = 50_000,
        };

        var star = new StarManager();
        var hero = new HeroManager(addresses, frida, bridge, star, NullLogger<HeroManager>.Instance);
        var map = new MapManager(addresses, frida, star, hero, NullLogger<MapManager>.Instance);
        var entitiesApi = new EntitiesApi();
        var box = CreateBox(bridge, 1300, 1000);
        entitiesApi.ReplaceSnapshot([], [], [box], [], []);
        var stats = new StatsManager(addresses, frida);
        var movement = new MovementApi(addresses, bridge, map, hero, NullLogger<MovementApi>.Instance);
        hero.Tick();
        map.Tick();
        stats.Tick(true);

        var context = new ModuleContext(hero, movement, entitiesApi, stats, config, map);
        var runner = new BotModuleRunner(
            context,
            config,
            new SafetyFinder(context),
            NullLogger<BotModuleRunner>.Instance);

        runner.Tick(isRunning: true, heroValid: true);

        Assert.NotNull(runner.ActiveModuleStatus);
        Assert.Single(bridge.MoveToCalls);
    }
}
