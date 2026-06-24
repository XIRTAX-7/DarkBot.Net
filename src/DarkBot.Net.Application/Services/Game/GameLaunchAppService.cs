using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Services.Game;

public sealed class GameLaunchAppService(
    IGameLauncherService launcher,
    IGameConnection game,
    IHostApplicationLifetime lifetime,
    ILogger<GameLaunchAppService> logger) : Contracts.IGameLaunchAppService
{
    private int _running;

    public void ScheduleLaunch(GameLaunchParameters launch) =>
        _ = LaunchAndConnectAsync(launch, lifetime.ApplicationStopping);

    public async Task LaunchAndConnectAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default)
    {
        if (game.IsValid && game.Phase == GameConnectionPhase.Connected)
        {
            logger.LogDebug("Game already connected — skipping launch");
            return;
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            if (game.Phase != GameConnectionPhase.Failed)
            {
                logger.LogDebug("Game launch already in progress — skipping duplicate");
                return;
            }

            logger.LogInformation("Previous game connect failed — retrying launch/connect");
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Game launch cancelled during shutdown");
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
