using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

/// <summary>
/// Идемпотентная остановка бота и Unity-клиента для всех триггеров shutdown.
/// </summary>
public sealed class GameShutdownCoordinator
{
    private readonly object _gate = new();
    private Task? _stopTask;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly IBotController _bot;
    private readonly UnityFridaGameApi _unityFrida;
    private readonly ILogger<GameShutdownCoordinator> _logger;
    private readonly GameClientLifecycle _lifecycle;

    public GameShutdownCoordinator(
        UnityGameLauncher unityLauncher,
        IBotController bot,
        UnityFridaGameApi unityFrida,
        GameClientLifecycle lifecycle,
        ILogger<GameShutdownCoordinator> logger)
    {
        _unityLauncher = unityLauncher;
        _bot = bot;
        _unityFrida = unityFrida;
        _lifecycle = lifecycle;
        _logger = logger;
    }

    public Task StopGameClientAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _stopTask ??= StopCoreAsync(cancellationToken);
            return _stopTask;
        }
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        _lifecycle.MarkIntentionalShutdown();
        _logger.LogInformation("Game shutdown coordinator started");

        try
        {
            _bot.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BotController.Stop failed, continuing game client shutdown");
        }

        try
        {
            _unityFrida.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UnityFridaGameApi dispose failed");
        }

        try
        {
            await _unityLauncher.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unity game shutdown failed");
        }

        _logger.LogInformation("Game shutdown coordinator completed");
    }
}
