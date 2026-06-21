namespace DarkBot.Net.Api.Game;

/// <summary>Entity row from Frida bridge snapshot (in-process read).</summary>
public sealed record FridaEntitySnapshot(
    int Id,
    double X,
    double Y,
    string Kind);

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
