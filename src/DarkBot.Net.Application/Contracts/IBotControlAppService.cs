namespace DarkBot.Net.Application.Contracts;

public interface IBotControlAppService
{
    bool IsRunning { get; }
    long TickCount { get; }
    double LastTickMs { get; }
    void Start();
    void Pause();
    void Stop();
}
