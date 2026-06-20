namespace DarkBot.Net.Core.Bot;

public interface IBotController
{
    bool IsRunning { get; }
    long TickCount { get; }
    double LastTickMs { get; }
    void Start();
    void Pause();
    void Stop();
}
