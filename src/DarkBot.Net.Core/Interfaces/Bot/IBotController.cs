namespace DarkBot.Net.Core.Interfaces.Bot;

/// <summary>Управление bot loop (start/pause/stop) и метрики tick.</summary>
public interface IBotController
{
    bool IsRunning { get; }
    long TickCount { get; }
    double LastTickMs { get; }

    /// <summary>Фактический период итерации loop (работа tick + delay), мс.</summary>
    double LastLoopPeriodMs { get; }

    void Start();
    void Pause();
    void Stop();
}
