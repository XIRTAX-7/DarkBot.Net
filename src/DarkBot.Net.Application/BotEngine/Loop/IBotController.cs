namespace DarkBot.Net.Application.BotEngine.Loop;

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
