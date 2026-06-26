using AppBot = DarkBot.Net.Application.Models.Bot;
using DarkBot.Net.Presentation.Models.Main;
using DarkBot.Net.Presentation.Models.Main.Map;

namespace DarkBot.Net.Presentation.Mapping;

public static class BotUiSnapshotMapper
{
    public static BotUiSnapshot ToUiSnapshot(AppBot.BotStatusSnapshot status) =>
        new(
            status.BotRunning,
            status.TickCount,
            status.LastTickMs,
            status.Credits,
            status.Uridium,
            status.Experience,
            status.Honor,
            status.Ping,
            ToMapRenderSnapshot(status.Map));

    private static MapRenderSnapshot ToMapRenderSnapshot(AppBot.MapStatusSnapshot map)
    {
        var settings = new MapRenderSettings(
            RoundEntities: true,
            TrailLengthSec: 15,
            MapZoom: 1.0,
            DisplayFlags: MapDisplayDefaults.JavaDefaults,
            CustomBackground: false,
            CustomBackgroundOpacity: 0.3f);

        if (map.MapId < 0)
            return MapRenderSnapshot.Loading with { Overlay = ToOverlay(map.Overlay), Settings = settings };

        return new MapRenderSnapshot(
            map.MapId,
            map.MapName,
            map.MapWidth,
            map.MapHeight,
            ToHero(map.Hero),
            map.Pet is null ? null : ToPet(map.Pet),
            map.Target is null ? null : ToTarget(map.Target),
            ToEntities(map.Entities),
            ToZones(map.Zones),
            ToOverlay(map.Overlay),
            settings);
    }

    private static MapHeroSnapshot ToHero(AppBot.MapHeroSnapshot hero) =>
        new(
            hero.Valid,
            hero.OnMap,
            hero.Id,
            hero.X,
            hero.Y,
            hero.Hp,
            hero.MaxHp,
            hero.Shield,
            hero.MaxShield,
            hero.Nano,
            hero.MaxNano,
            hero.Configuration,
            hero.Name,
            hero.PathSegments.Select(p => new MapPointSnapshot(p.X, p.Y)).ToArray(),
            hero.Destination is null ? null : new MapPointSnapshot(hero.Destination.X, hero.Destination.Y),
            hero.ViewBounds.Select(p => new MapPointSnapshot(p.X, p.Y)).ToArray());

    private static MapPetSnapshot ToPet(AppBot.MapPetSnapshot pet) =>
        new(pet.Valid, pet.X, pet.Y, pet.Hp, pet.MaxHp, pet.Fuel, pet.MaxFuel,
            pet.TargetName, pet.TargetHp, pet.TargetMaxHp);

    private static MapTargetSnapshot ToTarget(AppBot.MapTargetSnapshot target) =>
        new(
            target.Id,
            target.X,
            target.Y,
            target.Hp,
            target.MaxHp,
            target.Name,
            target.IsEnemy,
            target.Destination is null ? null : new MapPointSnapshot(target.Destination.X, target.Destination.Y));

    private static MapEntityCollections ToEntities(AppBot.MapEntityCollections entities) =>
        new(
            entities.Npcs.Select(ToEntity).ToArray(),
            entities.Boxes.Select(ToEntity).ToArray(),
            entities.Mines.Select(ToEntity).ToArray(),
            entities.Players.Select(ToEntity).ToArray(),
            entities.Pets.Select(ToEntity).ToArray(),
            entities.Relays.Select(ToEntity).ToArray(),
            entities.SpaceBalls.Select(ToEntity).ToArray(),
            entities.StaticEntities.Select(ToEntity).ToArray(),
            entities.Portals.Select(ToEntity).ToArray(),
            entities.BattleStations.Select(ToEntity).ToArray(),
            entities.Stations.Select(ToEntity).ToArray());

    private static MapEntitySnapshot ToEntity(AppBot.MapEntitySnapshot entity) =>
        new(
            entity.Id,
            entity.X,
            entity.Y,
            (MapEntityKind)(int)entity.Kind,
            entity.Fill,
            entity.Label,
            entity.IsEnemy,
            entity.IsGroupMember,
            entity.SubKind);

    private static MapZonesSnapshot ToZones(AppBot.MapZonesSnapshot zones) =>
        new(
            zones.Barriers.Select(ToPolygon).ToArray(),
            zones.Mists.Select(ToPolygon).ToArray(),
            [],
            [],
            zones.SafetyCircles.Select(s => new MapSafetyZoneSnapshot(s.X, s.Y, s.DiameterGame)).ToArray());

    private static MapPolygonZoneSnapshot ToPolygon(AppBot.MapPolygonZoneSnapshot zone) =>
        new(zone.Kind, zone.Polygon.Select(p => new MapPointSnapshot(p.X, p.Y)).ToArray());

    private static MapOverlaySnapshot ToOverlay(AppBot.MapOverlaySnapshot overlay) =>
        new(
            overlay.MapName,
            overlay.NextMapName,
            overlay.StatusLine,
            overlay.ModuleStatusLines,
            overlay.Sid,
            overlay.GroupMembers.Select(ToGroupMember).ToArray(),
            overlay.Boosters.Select(b => new MapBoosterSnapshot(b.Text, b.ColorArgb)).ToArray());

    private static MapGroupMemberSnapshot ToGroupMember(AppBot.MapGroupMemberSnapshot member) =>
        new(
            member.DisplayText,
            member.IsLeader,
            member.IsDead,
            member.IsLocked,
            member.IsCloaked,
            member.Hp,
            member.MaxHp,
            member.TargetHp,
            member.TargetMaxHp,
            member.HasTarget);
}
