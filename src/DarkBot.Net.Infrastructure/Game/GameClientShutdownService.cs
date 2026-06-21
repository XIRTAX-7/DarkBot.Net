using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Stops Darkorbit-client when the bot host shuts down.</summary>
public sealed class GameClientShutdownService : IHostedService
{
    private readonly DarkorbitClientLauncher _clientLauncher;
    private readonly ILogger<GameClientShutdownService> _logger;

    public GameClientShutdownService(
        DarkorbitClientLauncher clientLauncher,
        ILogger<GameClientShutdownService> logger)
    {
        _clientLauncher = clientLauncher;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down game client");
        _clientLauncher.Stop();
        return Task.CompletedTask;
    }
}
