using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Application.BotEngine.Runtime;
using DarkBot.Net.Application.BotEngine.Safety;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Application.Mappers.Bot;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Interfaces.Bot;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public sealed class BotStatusSnapshotMapperTests
{
    private static (
        HeroManager Hero,
        MapManager Map,
        EntityManager Entities,
        FakeGameFridaProbe Frida,
        StatsManager Stats,
        MovementApi Movement,
        BotModuleRunner Modules) CreateStack(Action<FakeGameFridaProbe>? configure = null)
    {
        var addresses = new BotAddressRegistry();
        var config = FakeConfigApi.WithCollectorDefaults();
        var frida = new FakeGameFridaProbe
        {
            MapId = 16,
            HeroId = 42,
            HeroHp = 150_000,
            HeroMaxHp = 200_000,
            HeroShield = 10_000,
            HeroMaxShield = 20_000,
            HeroX = 500,
            HeroY = 600,
        };
        configure?.Invoke(frida);

        var bridge = new FakeGameConnection();
        var star = new StarManager();
        var hero = new HeroManager(addresses, frida, bridge, star, NullLogger<HeroManager>.Instance);
        var map = new MapManager(addresses, frida, star, hero, NullLogger<MapManager>.Instance);
        var entitiesApi = new EntitiesApi();
        var entities = new EntityManager(addresses, map, frida, bridge, entitiesApi, config);
        var stats = new StatsManager(addresses, frida);
        var movement = new MovementApi(addresses, bridge, map, hero, NullLogger<MovementApi>.Instance);
        var context = new ModuleContext(hero, movement, entitiesApi, stats, config, map);
        var modules = new BotModuleRunner(
            context,
            config,
            new SafetyFinder(context),
            NullLogger<BotModuleRunner>.Instance);

        addresses.SetScreenManagerAddress(0x1000);
        hero.Tick();
        map.Tick();
        entities.Tick();
        stats.Tick(false);

        return (hero, map, entities, frida, stats, movement, modules);
    }

    [Fact]
    public void Create_TargetNull_WhenNoSelectedTarget()
    {
        var (hero, map, entities, frida, stats, movement, modules) = CreateStack();
        var bot = new StubBotController();

        var snapshot = BotStatusSnapshotMapper.Create(hero, map, entities, frida, stats, bot, movement, modules);

        Assert.Null(snapshot.Map.Target);
    }

    [Fact]
    public void Create_MapTarget_FromSelectedTargetWithShield()
    {
        var (hero, map, entities, frida, stats, movement, modules) = CreateStack(f =>
        {
            f.SelectedTarget = new FridaSelectedTargetSnapshot(
                999,
                5_000,
                10_000,
                800,
                800,
                "71",
                IsEnemy: true,
                X: 1200,
                Y: 1300,
                IsOnMap: true);
            f.Entities =
            [
                new FridaEntitySnapshot(999, 1200, 1300, "npc", false, "Streuner", true, false, null),
            ];
            f.EntityCount = 1;
        });
        entities.Tick();
        var bot = new StubBotController();

        var snapshot = BotStatusSnapshotMapper.Create(hero, map, entities, frida, stats, bot, movement, modules);
        var target = snapshot.Map.Target;

        Assert.NotNull(target);
        Assert.Equal(999, target!.Id);
        Assert.Equal(800, target.Shield);
        Assert.Equal(800, target.MaxShield);
        Assert.Equal("71", target.Name);
        Assert.Single(snapshot.Map.Entities.Npcs);
    }

    [Fact]
    public void Create_OffMapTarget_StillShowsTargetSectionWithoutNpcOnMap()
    {
        var (hero, map, entities, frida, stats, movement, modules) = CreateStack(f =>
        {
            f.SelectedTarget = new FridaSelectedTargetSnapshot(
                999,
                5_000,
                10_000,
                800,
                800,
                "71",
                IsEnemy: true,
                X: 0,
                Y: 0,
                IsOnMap: false);
            f.Entities = [];
            f.EntityCount = 0;
        });
        entities.Tick();
        var bot = new StubBotController();

        var snapshot = BotStatusSnapshotMapper.Create(hero, map, entities, frida, stats, bot, movement, modules);

        Assert.NotNull(snapshot.Map.Target);
        Assert.Equal(999, snapshot.Map.Target!.Id);
        Assert.Empty(snapshot.Map.Entities.Npcs);
    }

    [Fact]
    public void Create_MapTarget_WhenOnlyShieldKnown()
    {
        var (hero, map, entities, frida, stats, movement, modules) = CreateStack(f =>
        {
            f.SelectedTarget = new FridaSelectedTargetSnapshot(
                999,
                Hp: 0,
                MaxHp: 0,
                Shield: 400,
                MaxShield: 0,
                Name: "71",
                IsEnemy: true,
                X: 1200,
                Y: 1300,
                IsOnMap: true);
            f.Entities =
            [
                new FridaEntitySnapshot(999, 1200, 1300, "npc", false, "Streuner", true, false, null),
            ];
            f.EntityCount = 1;
        });
        entities.Tick();
        var bot = new StubBotController();

        var snapshot = BotStatusSnapshotMapper.Create(hero, map, entities, frida, stats, bot, movement, modules);

        Assert.NotNull(snapshot.Map.Target);
        Assert.Equal(400, snapshot.Map.Target!.Shield);
        Assert.Equal(0, snapshot.Map.Target.MaxShield);
    }

    private sealed class StubBotController : IBotController
    {
        public bool IsRunning => false;

        public long TickCount => 0;

        public double LastTickMs => 0;

        public double LastLoopPeriodMs => 0;

        public void Start() { }

        public void Pause() { }

        public void Stop() { }
    }
}
