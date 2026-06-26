namespace DarkBot.Net.Application.Models.Bot;

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
}
