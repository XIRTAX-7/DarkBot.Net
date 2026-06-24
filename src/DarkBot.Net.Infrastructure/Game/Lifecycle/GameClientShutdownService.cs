using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

/// <summary>Останавливает Unity-клиент при shutdown host.</summary>
public sealed class GameClientShutdownService : IHostedService
{
    private readonly GameShutdownCoordinator _coordinator;
    private readonly ILogger<GameClientShutdownService> _logger;

    public GameClientShutdownService(
        GameShutdownCoordinator coordinator,
        ILogger<GameClientShutdownService> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Host shutdown: stopping game client via coordinator");
        await _coordinator.StopGameClientAsync(cancellationToken).ConfigureAwait(false);
    }
}
