using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

/// <summary>Полный перезапуск Unity-клиента (kill + spawn + connect) без остановки бота.</summary>
public sealed class GameClientRestartService : IGameClientRestartAppService
{
    private readonly IGameLauncherService _launcher;
    private readonly GameLaunchSessionResolver _sessionResolver;
    private readonly BotAddressRegistry _addresses;
    private readonly GameClientLifecycle _lifecycle;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientRestartService> _logger;
    private readonly SemaphoreSlim _restartLock = new(1, 1);
    private long _lastRestartMs;
    private int _restartInProgress;

    public GameClientRestartService(
        IGameLauncherService launcher,
        GameLaunchSessionResolver sessionResolver,
        BotAddressRegistry addresses,
        GameClientLifecycle lifecycle,
        IHostApplicationLifetime hostLifetime,
        IOptions<GameApiOptions> options,
        ILogger<GameClientRestartService> logger)
    {
        _launcher = launcher;
        _sessionResolver = sessionResolver;
        _addresses = addresses;
        _lifecycle = lifecycle;
        _hostLifetime = hostLifetime;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsRestartInProgress => Volatile.Read(ref _restartInProgress) != 0;

    public bool CanRestart =>
        !_lifecycle.IntentionalShutdown
        && !_hostLifetime.ApplicationStopping.IsCancellationRequested
        && _sessionResolver.HasLaunchSession
        && !IsRestartInProgress;

    public Task RestartClientAsync(CancellationToken cancellationToken = default) =>
        RestartClientCoreAsync(cancellationToken, respectCooldown: false);

    internal Task TryAutoRestartAsync(string reason, CancellationToken cancellationToken = default) =>
        RestartClientCoreAsync(cancellationToken, respectCooldown: true, reason: reason);

    private async Task RestartClientCoreAsync(
        CancellationToken cancellationToken,
        bool respectCooldown,
        string? reason = null)
    {
        if (_lifecycle.IntentionalShutdown
            || _hostLifetime.ApplicationStopping.IsCancellationRequested)
        {
            return;
        }

        if (respectCooldown && !IsCooldownElapsed())
        {
            _logger.LogDebug("Skipping auto-restart ({Reason}) — cooldown active", reason);
            return;
        }

        if (!await _restartLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Restart already in progress — skipping ({Reason})", reason ?? "manual");
            return;
        }

        Interlocked.Exchange(ref _restartInProgress, 1);
        try
        {
            var launch = await _sessionResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
            if (launch is null)
            {
                _logger.LogWarning("Cannot restart Unity game — no launch session");
                return;
            }

            _logger.LogInformation(
                "Restarting Unity game{Reason}",
                reason is null ? string.Empty : $" ({reason})");

            _addresses.MarkInvalid();
            var result = await _launcher.RestartClientAsync(launch, cancellationToken).ConfigureAwait(false);
            _lastRestartMs = Environment.TickCount64;

            if (result.Success)
                _logger.LogInformation("Unity game restart OK — pid {Pid}", result.PepperPid);
            else
                _logger.LogWarning("Unity game restart connect failed: {Error}", result.Error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Unity game restart cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unity game restart failed");
        }
        finally
        {
            Interlocked.Exchange(ref _restartInProgress, 0);
            _restartLock.Release();
        }
    }

    private bool IsCooldownElapsed()
    {
        var cooldownMs = _options.ClientAutoRestartCooldownSec * 1000L;
        return Environment.TickCount64 - _lastRestartMs >= cooldownMs;
    }
}
