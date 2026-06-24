using DarkBot.Net.Presentation.Controls;

namespace DarkBot.Net.Presentation.Services;

public sealed record MapPointSnapshot(double X, double Y);

public sealed record MapPolygonZoneSnapshot(string Kind, IReadOnlyList<MapPointSnapshot> Polygon);

public sealed record MapSafetyZoneSnapshot(double X, double Y, double DiameterGame);

public sealed record MapHeroSnapshot(
    bool Valid,
    bool OnMap,
    int Id,
    double X,
    double Y,
    int Hp,
    int MaxHp,
    int Shield,
    int MaxShield,
    int Nano,
    int MaxNano,
    string Configuration,
    string? Name,
    IReadOnlyList<MapPointSnapshot> PathSegments,
    MapPointSnapshot? Destination,
    IReadOnlyList<MapPointSnapshot> ViewBounds);

public sealed record MapPetSnapshot(
    bool Valid,
    double X,
    double Y,
    int Hp,
    int MaxHp,
    int Fuel,
    int MaxFuel,
    string? TargetName,
    int TargetHp,
    int TargetMaxHp);

public sealed record MapTargetSnapshot(
    int Id,
    double X,
    double Y,
    int Hp,
    int MaxHp,
    string? Name,
    bool IsEnemy,
    MapPointSnapshot? Destination);

public sealed record MapEntityCollections(
    IReadOnlyList<MapEntitySnapshot> Npcs,
    IReadOnlyList<MapEntitySnapshot> Boxes,
    IReadOnlyList<MapEntitySnapshot> Mines,
    IReadOnlyList<MapEntitySnapshot> Players,
    IReadOnlyList<MapEntitySnapshot> Pets,
    IReadOnlyList<MapEntitySnapshot> Relays,
    IReadOnlyList<MapEntitySnapshot> SpaceBalls,
    IReadOnlyList<MapEntitySnapshot> StaticEntities,
    IReadOnlyList<MapEntitySnapshot> Portals,
    IReadOnlyList<MapEntitySnapshot> BattleStations,
    IReadOnlyList<MapEntitySnapshot> Stations)
{
    public static MapEntityCollections Empty { get; } = new(
        [], [], [], [], [], [], [], [], [], [], []);
}

public sealed record MapZonesSnapshot(
    IReadOnlyList<MapPolygonZoneSnapshot> Barriers,
    IReadOnlyList<MapPolygonZoneSnapshot> Mists,
    IReadOnlyList<MapZoneCell> PreferGrid,
    IReadOnlyList<MapZoneCell> AvoidGrid,
    IReadOnlyList<MapSafetyZoneSnapshot> SafetyCircles)
{
    public static MapZonesSnapshot Empty { get; } = new([], [], [], [], []);
}

public sealed record MapGroupMemberSnapshot(
    string DisplayText,
    bool IsLeader,
    bool IsDead,
    bool IsLocked,
    bool IsCloaked,
    int Hp,
    int MaxHp,
    int TargetHp,
    int TargetMaxHp,
    bool HasTarget);

public sealed record MapBoosterSnapshot(string Text, uint ColorArgb);

public sealed record MapOverlaySnapshot(
    string MapName,
    string? NextMapName,
    string StatusLine,
    IReadOnlyList<string> ModuleStatusLines,
    string? Sid,
    IReadOnlyList<MapGroupMemberSnapshot> GroupMembers,
    IReadOnlyList<MapBoosterSnapshot> Boosters);

public sealed record MapRenderSettings(
    bool RoundEntities,
    int TrailLengthSec,
    double MapZoom,
    MapDisplayFlag DisplayFlags,
    bool CustomBackground,
    float CustomBackgroundOpacity);

public sealed record MapRenderSnapshot(
    int MapId,
    string MapName,
    int MapWidth,
    int MapHeight,
    MapHeroSnapshot Hero,
    MapPetSnapshot? Pet,
    MapTargetSnapshot? Target,
    MapEntityCollections Entities,
    MapZonesSnapshot Zones,
    MapOverlaySnapshot Overlay,
    MapRenderSettings Settings)
{
    public static MapRenderSnapshot Loading { get; } = CreateLoading();

    private static MapRenderSnapshot CreateLoading() =>
        new(
            MapId: -1,
            MapName: "Загрузка",
            MapWidth: 21000,
            MapHeight: 13500,
            Hero: new MapHeroSnapshot(
                false, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, "?", null, [], null, []),
            Pet: null,
            Target: null,
            Entities: MapEntityCollections.Empty,
            Zones: MapZonesSnapshot.Empty,
            Overlay: new MapOverlaySnapshot(
                "Загрузка", null, "WAITING", [], null, [], []),
            Settings: new MapRenderSettings(
                RoundEntities: true,
                TrailLengthSec: 15,
                MapZoom: 1.0,
                DisplayFlags: MapDisplayDefaults.JavaDefaults,
                CustomBackground: false,
                CustomBackgroundOpacity: 0.3f));
}
