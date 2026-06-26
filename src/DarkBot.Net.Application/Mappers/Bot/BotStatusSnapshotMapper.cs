using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Stats;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Mappers.Bot;

internal static class BotStatusSnapshotMapper
{
    public static BotStatusSnapshot Create(
        HeroManager hero,
        MapManager map,
        EntityManager entities,
        IGameFridaProbe frida,
        IStatsApi stats,
        IBotController bot,
        IMovementApi movement) =>
        new(
            BotRunning: bot.IsRunning,
            TickCount: bot.TickCount,
            LastTickMs: bot.LastTickMs,
            Credits: stats.GetStatValue(Stats.General.Credits),
            Uridium: stats.GetStatValue(Stats.General.Uridium),
            Experience: stats.GetStatValue(Stats.General.Experience),
            Honor: stats.GetStatValue(Stats.General.Honor),
            Ping: stats.Ping,
            Map: CreateMapStatusSnapshot(hero, map, entities, frida, stats, bot, movement));

    private static MapStatusSnapshot CreateMapStatusSnapshot(
        HeroManager hero,
        MapManager map,
        EntityManager entities,
        IGameFridaProbe frida,
        IStatsApi stats,
        IBotController bot,
        IMovementApi movement)
    {
        var health = hero.Health;
        var mapId = map.MapId;
        var mapName = hero.Map.Name;
        var running = bot.IsRunning;
        var runtime = stats.RunningTime;

        var heroSnapshot = new MapHeroSnapshot(
            Valid: hero.IsValid,
            OnMap: hero.HasMapPosition,
            Id: hero.ShipId,
            X: hero.X,
            Y: hero.Y,
            Hp: health.Hp,
            MaxHp: health.MaxHp,
            Shield: 0,
            MaxShield: 0,
            Nano: 0,
            MaxNano: 0,
            Configuration: hero.ActiveConfiguration.ToString(),
            Name: null,
            PathSegments: movement.Path
                .Select(p => new MapPointSnapshot(p.X, p.Y))
                .ToArray(),
            Destination: movement.Destination.X > 0 || movement.Destination.Y > 0
                ? new MapPointSnapshot(movement.Destination.X, movement.Destination.Y)
                : null,
            ViewBounds: []);

        var entityCollections = CreateEntityCollections(entities, map, mapName);

        var overlay = new MapOverlaySnapshot(
            MapName: mapName,
            NextMapName: null,
            StatusLine: running
                ? $"RUNNING {runtime:hh\\:mm\\:ss}"
                : $"WAITING {runtime:hh\\:mm\\:ss}",
            ModuleStatusLines: [],
            Sid: null,
            GroupMembers: [],
            Boosters: []);

        if (mapId < 0)
            return MapStatusSnapshot.Loading with { Overlay = overlay };

        return new MapStatusSnapshot(
            mapId,
            mapName,
            map.InternalWidth,
            map.InternalHeight,
            heroSnapshot,
            Pet: null,
            Target: null,
            entityCollections,
            new MapZonesSnapshot(
                Barriers: CreateAgentZones(frida.Zones, "barrier"),
                Mists: CreateAgentZones(frida.Zones, "mist"),
                SafetyCircles: []),
            overlay);
    }

    private static MapEntityCollections CreateEntityCollections(
        EntityManager entities,
        MapManager map,
        string mapName)
    {
        static MapEntitySnapshot ToSnapshot(FridaEntitySnapshot entity) =>
            new(
                entity.Id,
                entity.X,
                entity.Y,
                MapEntityKindMapper.FromAgentKind(entity.Kind),
                entity.Fill,
                entity.Label,
                entity.IsEnemy,
                entity.IsGroupMember,
                entity.SubKind);

        var all = entities.AllSnapshots;
        var portals = all.Where(e => e.Kind is "portal").Select(ToSnapshot).ToArray();
        if (portals.Length == 0 && map.Portals.Count > 0)
        {
            portals = map.Portals
                .Select((p, index) => new MapEntitySnapshot(
                    index + 1,
                    p.X,
                    p.Y,
                    MapEntityKind.Portal,
                    Label: p.TargetShortName))
                .ToArray();
        }

        return new MapEntityCollections(
            Npcs: all.Where(e => e.Kind is "npc").Select(ToSnapshot).ToArray(),
            Boxes: all.Where(e => e.Kind is "box").Select(ToSnapshot).ToArray(),
            Mines: all.Where(e => e.Kind is "mine").Select(ToSnapshot).ToArray(),
            Players: all.Where(e => e.Kind is "player" or "ship").Select(ToSnapshot).ToArray(),
            Pets: all.Where(e => e.Kind is "pet").Select(ToSnapshot).ToArray(),
            Relays: all.Where(e => e.Kind is "relay").Select(ToSnapshot).ToArray(),
            SpaceBalls: all.Where(e => e.Kind is "space_ball" or "spaceball").Select(ToSnapshot).ToArray(),
            StaticEntities: all.Where(e => e.Kind is "static").Select(ToSnapshot).ToArray(),
            Portals: portals,
            BattleStations: all.Where(e => e.Kind is "battle_station").Select(ToSnapshot).ToArray(),
            Stations: MapJavaStationFallback.MergeStations(
                all.Where(e => e.Kind is "station_turret" or "base_spot").Select(ToSnapshot).ToArray(),
                mapName,
                map.InternalWidth,
                map.InternalHeight));
    }

    private static IReadOnlyList<MapPolygonZoneSnapshot> CreateAgentZones(
        IReadOnlyList<FridaBridgeZoneSnapshot> zones,
        string kind) =>
        zones
            .Where(z => string.Equals(z.Kind, kind, StringComparison.OrdinalIgnoreCase))
            .Select(z => new MapPolygonZoneSnapshot(
                z.Kind,
                z.Polygon.Select(p => new MapPointSnapshot(p.X, p.Y)).ToArray()))
            .ToArray();
}
