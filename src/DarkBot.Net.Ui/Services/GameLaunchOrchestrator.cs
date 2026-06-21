using DarkBot.Net.Agent.Windows.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Ui.Services;

/// <summary>Background launch + Frida connect — shared by Login and auto-launch on startup.</summary>
public sealed class GameLaunchOrchestrator
{
    private readonly GameLauncherService _launcher;
    private readonly IGameConnection _game;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameLaunchOrchestrator> _logger;
    private int _running;

    public GameLaunchOrchestrator(
        GameLauncherService launcher,
        IGameConnection game,
        IOptions<GameApiOptions> options,
        ILogger<GameLaunchOrchestrator> logger)
    {
        _launcher = launcher;
        _game = game;
        _options = options.Value;
        _logger = logger;
    }

    public void ScheduleLaunch(GameLaunchParameters launch)
    {
        if (_options.BrowserApi == GameApiMode.BackpageOnly)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await LaunchAndConnectAsync(launch, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background game launch failed");
            }
        });
    }

    public async Task LaunchAndConnectAsync(
        GameLaunchParameters launch,
        CancellationToken cancellationToken = default)
    {
        if (_options.BrowserApi == GameApiMode.BackpageOnly)
            return;

        if (_game.IsValid && _game.Phase == GameConnectionPhase.Connected)
        {
            _logger.LogDebug("Game already connected — skipping launch");
            return;
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            _logger.LogDebug("Game launch already in progress — skipping duplicate");
            return;
        }

        try
        {
            await _launcher.LaunchAsync(launch, cancellationToken).ConfigureAwait(false);
            var result = await _launcher.ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (result.Success)
                _logger.LogInformation("Game connect OK — Pepper pid {Pid}", result.PepperPid);
            else
                _logger.LogWarning("Game connect failed: {Error}", result.Error);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
