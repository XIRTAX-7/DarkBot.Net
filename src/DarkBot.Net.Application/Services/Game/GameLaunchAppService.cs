using DarkBot.Net.Application.Bot;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Services.Game;

public sealed class GameLaunchAppService(
    IGameLauncherService launcher,
    IGameConnection game,
    IOptions<GameApiOptions> options,
    ILogger<GameLaunchAppService> logger) : Contracts.IGameLaunchAppService
{
    private readonly GameApiOptions _options = options.Value;
    private int _running;

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
                logger.LogWarning(ex, "Background game launch failed");
            }
        });
    }

    public async Task LaunchAndConnectAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default)
    {
        if (_options.BrowserApi == GameApiMode.BackpageOnly)
            return;

        if (game.IsValid && game.Phase == GameConnectionPhase.Connected)
        {
            logger.LogDebug("Game already connected — skipping launch");
            return;
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            logger.LogDebug("Game launch already in progress — skipping duplicate");
            return;
        }

        try
        {
            await launcher.LaunchAsync(launch, cancellationToken).ConfigureAwait(false);
            var result = await launcher.ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (result.Success)
                logger.LogInformation("Game connect OK — Pepper pid {Pid}", result.PepperPid);
            else
                logger.LogWarning("Game connect failed: {Error}", result.Error);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
