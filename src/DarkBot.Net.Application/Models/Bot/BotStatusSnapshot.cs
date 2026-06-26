namespace DarkBot.Net.Application.Models.Bot;

public sealed record BotStatusSnapshot(
    bool BotRunning,
    long TickCount,
    double LastTickMs,
    double Credits,
    double Uridium,
    double Experience,
    double Honor,
    int Ping,
    MapStatusSnapshot Map);
