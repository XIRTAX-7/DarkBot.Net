using DarkBot.Net.Application.Bot;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Идемпотентная остановка бота и Darkorbit-client для всех триггеров shutdown.
/// Все вызовы получают одну memoized <see cref="Task"/>.
/// </summary>
public sealed class GameShutdownCoordinator
{
    private readonly object _gate = new();
    private Task? _stopTask;
    private readonly DarkorbitClientLauncher _launcher;
    private readonly IBotController _bot;
    private readonly FridaGameApi _frida;
    private readonly ElectronControlClient _control;
    private readonly ILogger<GameShutdownCoordinator> _logger;
    private readonly GameClientLifecycle _lifecycle;

    public GameShutdownCoordinator(
        DarkorbitClientLauncher launcher,
        IBotController bot,
        FridaGameApi frida,
        ElectronControlClient control,
        GameClientLifecycle lifecycle,
        ILogger<GameShutdownCoordinator> logger)
    {
        _launcher = launcher;
        _bot = bot;
        _frida = frida;
        _control = control;
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
            await _launcher.StopAsync(cancellationToken).ConfigureAwait(false);
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
            _frida.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FridaGameApi dispose failed");
        }

        _logger.LogInformation("Game shutdown coordinator completed");
    }
}
