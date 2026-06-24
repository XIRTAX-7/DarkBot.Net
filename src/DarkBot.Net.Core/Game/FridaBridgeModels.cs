namespace DarkBot.Net.Core.Game;

/// <summary>Entity row from Frida bridge snapshot (in-process read).</summary>
public sealed record FridaEntitySnapshot(
    int Id,
    double X,
    double Y,
    string Kind,
    bool Fill = false,
    string? Label = null,
    bool IsEnemy = false,
    bool IsGroupMember = false,
    string? SubKind = null);

public sealed record FridaBridgeZoneSnapshot(
    string Kind,
    IReadOnlyList<MapPointSnapshotCore> Polygon);

public sealed record MapPointSnapshotCore(double X, double Y);

/// <summary>Session stats from Frida bridge (heroInfo closure).</summary>
public sealed record FridaStatsSnapshot(
    int UserId,
    long Credits,
    long Uridium,
    long Experience,
    long Honor,
    int Cargo,
    int MaxCargo,
    int NovaEnergy);
