namespace DarkBot.Net.Application.DTOs.Responses.Bot;

/// <summary>Снимок состояния бота для Presentation.</summary>
public sealed record BotStatusSnapshot(
    bool BotRunning,
    long TickCount,
    double LastTickMs,
    /// <summary>Средний working set процесса бота, МБ.</summary>
    double MemoryMb,
    /// <summary>Эффективная частота bot loop, Гц (1000 / LastTickMs).</summary>
    double LoopHz,
    double Credits,
    double Uridium,
    double Experience,
    double Honor,
    int Ping,
    TimeSpan RunningTime,
    double EarnedCreditsPerHour,
    double EarnedUridiumPerHour,
    double EarnedExperiencePerHour,
    double EarnedHonorPerHour,
    int Cargo,
    int MaxCargo,
    MapStatusSnapshot Map)
{
    public static BotStatusSnapshot Empty { get; } = new(
        BotRunning: false,
        TickCount: 0,
        LastTickMs: 0,
        MemoryMb: 0,
        LoopHz: 0,
        Credits: 0,
        Uridium: 0,
        Experience: 0,
        Honor: 0,
        Ping: 0,
        RunningTime: TimeSpan.Zero,
        EarnedCreditsPerHour: 0,
        EarnedUridiumPerHour: 0,
        EarnedExperiencePerHour: 0,
        EarnedHonorPerHour: 0,
        Cargo: 0,
        MaxCargo: 0,
        Map: MapStatusSnapshot.Loading);
}
