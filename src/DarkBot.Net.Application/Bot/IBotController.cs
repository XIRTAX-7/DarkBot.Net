namespace DarkBot.Net.Application.Bot;

public interface IBotController
{
    bool IsRunning { get; }
    long TickCount { get; }
    double LastTickMs { get; }
    void Start();
    void Pause();
    void Stop();
}
