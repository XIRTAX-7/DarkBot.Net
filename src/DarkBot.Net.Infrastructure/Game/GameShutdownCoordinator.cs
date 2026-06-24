using DarkBot.Net.Application.Bot;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Идемпотентная остановка бота и игрового клиента для всех триггеров shutdown.
/// </summary>
public sealed class GameShutdownCoordinator
{
    private readonly object _gate = new();
    private Task? _stopTask;
    private readonly DarkorbitClientLauncher _legacyLauncher;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly IBotController _bot;
    private readonly FridaGameApi _legacyFrida;
    private readonly UnityFridaGameApi _unityFrida;
    private readonly ElectronControlClient _control;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameShutdownCoordinator> _logger;
    private readonly GameClientLifecycle _lifecycle;

    public GameShutdownCoordinator(
        DarkorbitClientLauncher legacyLauncher,
        UnityGameLauncher unityLauncher,
        IBotController bot,
        FridaGameApi legacyFrida,
        UnityFridaGameApi unityFrida,
        ElectronControlClient control,
        IOptions<GameApiOptions> options,
        GameClientLifecycle lifecycle,
        ILogger<GameShutdownCoordinator> logger)
    {
        _legacyLauncher = legacyLauncher;
        _unityLauncher = unityLauncher;
        _bot = bot;
        _legacyFrida = legacyFrida;
        _unityFrida = unityFrida;
        _control = control;
        _options = options.Value;
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

        if (_options.BrowserApi == GameApiMode.FridaClient)
        {
            try
            {
                await _legacyLauncher.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Darkorbit-client shutdown failed");
            }

            try
            {
                _control.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ElectronControlClient dispose failed");
            }

            try
            {
                _legacyFrida.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "FridaGameApi dispose failed");
            }
        }
        else if (_options.BrowserApi == GameApiMode.UnityClient)
        {
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
        }

        _logger.LogInformation("Game shutdown coordinator completed");
    }
}
