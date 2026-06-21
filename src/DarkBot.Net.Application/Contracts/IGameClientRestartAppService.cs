namespace DarkBot.Net.Application.Contracts;

public interface IGameClientRestartAppService
{
    bool CanRestart { get; }

    bool IsRestartInProgress { get; }

    Task RestartClientAsync(CancellationToken cancellationToken = default);
}
