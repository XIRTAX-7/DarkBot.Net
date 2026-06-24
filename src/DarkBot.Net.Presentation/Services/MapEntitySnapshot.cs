namespace DarkBot.Net.Presentation.Services;

public enum MapEntityKind
{
    Unknown,
    Npc,
    Box,
    Mine,
    Player,
    Pet,
    Relay,
    SpaceBall,
    Static,
    Portal,
    BattleStation,
    StationTurret,
    BaseSpot,
    Barrier,
    Mist,
    Other
}

public sealed record MapEntitySnapshot(
    int Id,
    double X,
    double Y,
    MapEntityKind Kind,
    bool Fill = false,
    string? Label = null,
    bool IsEnemy = false,
    bool IsGroupMember = false,
    string? SubKind = null);

public static class MapEntityKindMapping
{
    public static MapEntityKind FromAgentKind(string? kind) =>
        kind?.ToLowerInvariant() switch
        {
            "npc" => MapEntityKind.Npc,
            "box" => MapEntityKind.Box,
            "mine" => MapEntityKind.Mine,
            "player" or "ship" => MapEntityKind.Player,
            "pet" => MapEntityKind.Pet,
            "relay" => MapEntityKind.Relay,
            "space_ball" or "spaceball" => MapEntityKind.SpaceBall,
            "static" => MapEntityKind.Static,
            "portal" => MapEntityKind.Portal,
            "battle_station" => MapEntityKind.BattleStation,
            "station_turret" => MapEntityKind.StationTurret,
            "base_spot" or "station" => MapEntityKind.BaseSpot,
            "barrier" => MapEntityKind.Barrier,
            "mist" => MapEntityKind.Mist,
            _ => MapEntityKind.Unknown
        };

    public static string ToAgentKind(MapEntityKind kind) =>
        kind switch
        {
            MapEntityKind.Npc => "npc",
            MapEntityKind.Box => "box",
            MapEntityKind.Mine => "mine",
            MapEntityKind.Player => "player",
            MapEntityKind.Pet => "pet",
            MapEntityKind.Relay => "relay",
            MapEntityKind.SpaceBall => "space_ball",
            MapEntityKind.Static => "static",
            MapEntityKind.Portal => "portal",
            MapEntityKind.BattleStation => "battle_station",
            MapEntityKind.StationTurret => "station_turret",
            MapEntityKind.BaseSpot => "base_spot",
            MapEntityKind.Barrier => "barrier",
            MapEntityKind.Mist => "mist",
            _ => "other"
        };
}
