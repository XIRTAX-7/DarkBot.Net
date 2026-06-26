using DarkBot.Net.Application.DTOs.Responses.Bot;

namespace DarkBot.Net.Application.Mappers.Bot;

internal static class MapEntityKindMapper
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
