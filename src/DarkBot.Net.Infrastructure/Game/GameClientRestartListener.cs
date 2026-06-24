using DarkBot.Net.Application.Bot;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Реагирует на push-события от клиента (process exit, Frida bridge WS, phase) — без polling.
/// Долгое ожидание connect/reconnect обрабатывается одноразовым таймером, который ставится только при смене состояния.
/// </summary>
public sealed class GameClientRestartListener : IHostedService, IDisposable
{
    private readonly DarkorbitClientLauncher _legacyLauncher;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly IGameConnection _game;
    private readonly IBotController _bot;
    private readonly GameClientRestartService _restart;
    private readonly GameClientLifecycle _lifecycle;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientRestartListener> _logger;
    private readonly object _timerGate = new();
    private Timer? _recoveryTimer;
    private string? _pendingRecoveryReason;

    public GameClientRestartListener(
        DarkorbitClientLauncher legacyLauncher,
        UnityGameLauncher unityLauncher,
        IGameConnection game,
        IBotController bot,
        GameClientRestartService restart,
        GameClientLifecycle lifecycle,
        IHostApplicationLifetime hostLifetime,
        IOptions<GameApiOptions> options,
        ILogger<GameClientRestartListener> logger)
    {
        _legacyLauncher = legacyLauncher;
        _unityLauncher = unityLauncher;
        _game = game;
        _bot = bot;
        _restart = restart;
        _lifecycle = lifecycle;
        _hostLifetime = hostLifetime;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.BrowserApi == GameApiMode.BackpageOnly)
            return Task.CompletedTask;

        _legacyLauncher.ClientProcessExited += OnClientProcessExited;
        _unityLauncher.ClientProcessExited += OnClientProcessExited;
        _game.PhaseChanged += OnPhaseChanged;
        _game.BridgeDisconnected += OnBridgeDisconnected;

        _logger.LogDebug("Game client restart listener started (event-driven)");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Unsubscribe();
        CancelRecoveryTimer();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Unsubscribe();
        CancelRecoveryTimer();
    }

    private void Unsubscribe()
    {
        _legacyLauncher.ClientProcessExited -= OnClientProcessExited;
        _unityLauncher.ClientProcessExited -= OnClientProcessExited;
        _game.PhaseChanged -= OnPhaseChanged;
        _game.BridgeDisconnected -= OnBridgeDisconnected;
    }

    private void OnClientProcessExited(object? sender, EventArgs e)
    {
        if (!ShouldAutoRestart())
            return;

        CancelRecoveryTimer();
        _logger.LogInformation("Game client process exited — scheduling restart");
        ScheduleRestart("process exited");
    }

    private void OnBridgeDisconnected()
    {
        if (!ShouldAutoRestart())
            return;

        _logger.LogInformation("Frida bridge disconnected — arming client recovery timer");
        ArmRecoveryTimer("bridge disconnected");
    }

    private void OnPhaseChanged(GameConnectionPhase phase)
    {
        if (!ShouldAutoRestart())
        {
            CancelRecoveryTimer();
            return;
        }

        switch (phase)
        {
            case GameConnectionPhase.Connected:
                CancelRecoveryTimer();
                break;
            case GameConnectionPhase.Failed:
                CancelRecoveryTimer();
                _logger.LogWarning(
                    "Game connect failed ({Reason}) — scheduling client restart",
                    _game.LastFailureReason ?? "unknown");
                ScheduleRestart($"connect failed: {_game.LastFailureReason ?? "unknown"}");
                break;
            case GameConnectionPhase.Launching:
            case GameConnectionPhase.WaitingForGameLoad:
                ArmRecoveryTimer($"phase {phase}");
                break;
        }
    }

    private void ArmRecoveryTimer(string reason)
    {
        lock (_timerGate)
        {
            _pendingRecoveryReason = reason;
            _recoveryTimer?.Dispose();

            var delayMs = _options.ClientStuckConnectRestartSec * 1000;
            _recoveryTimer = new Timer(
                OnRecoveryTimerFired,
                null,
                delayMs,
                Timeout.Infinite);
        }

        _logger.LogDebug(
            "Client recovery timer armed ({Reason}, {DelaySec}s)",
            reason,
            _options.ClientStuckConnectRestartSec);
    }

    private void CancelRecoveryTimer()
    {
        lock (_timerGate)
        {
            _recoveryTimer?.Dispose();
            _recoveryTimer = null;
            _pendingRecoveryReason = null;
        }
    }

    private void OnRecoveryTimerFired(object? state)
    {
        if (!ShouldAutoRestart() || _game.IsValid)
            return;

        string reason;
        lock (_timerGate)
        {
            reason = _pendingRecoveryReason ?? "recovery timeout";
            _recoveryTimer = null;
            _pendingRecoveryReason = null;
        }

        _logger.LogWarning("Client recovery timer elapsed — scheduling restart ({Reason})", reason);
        ScheduleRestart(reason);
    }

    private void ScheduleRestart(string reason) =>
        _ = _restart.TryAutoRestartAsync(reason, _hostLifetime.ApplicationStopping);

    private bool ShouldAutoRestart() =>
        _options.BrowserApi != GameApiMode.BackpageOnly
        && !_lifecycle.IntentionalShutdown
        && !_hostLifetime.ApplicationStopping.IsCancellationRequested
        && _bot.IsRunning;
}
