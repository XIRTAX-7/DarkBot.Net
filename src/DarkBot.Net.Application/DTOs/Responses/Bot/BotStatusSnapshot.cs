namespace DarkBot.Net.Application.DTOs.Responses.Bot;

/// <summary>Снимок состояния бота для Presentation.</summary>
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
