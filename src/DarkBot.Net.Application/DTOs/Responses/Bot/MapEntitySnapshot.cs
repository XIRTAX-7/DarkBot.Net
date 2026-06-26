namespace DarkBot.Net.Application.DTOs.Responses.Bot;

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
