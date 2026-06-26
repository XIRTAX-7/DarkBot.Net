namespace DarkBot.Net.Application.Contracts;

public interface IGameShutdownAppService
{
    Task StopGameClientAsync(CancellationToken cancellationToken = default);
}
