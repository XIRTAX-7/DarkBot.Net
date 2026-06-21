using DarkBot.Net.Application.Bot;

namespace DarkBot.Net.Application.Services.Bot;

public sealed class BotControlAppService(IBotController controller) : Contracts.IBotControlAppService
{
    public bool IsRunning => controller.IsRunning;
    public long TickCount => controller.TickCount;
    public double LastTickMs => controller.LastTickMs;

    public void Start() => controller.Start();

    public void Pause() => controller.Pause();

    public void Stop() => controller.Stop();
}
