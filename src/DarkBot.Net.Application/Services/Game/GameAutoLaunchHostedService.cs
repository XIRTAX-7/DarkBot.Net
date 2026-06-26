using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.Services.Game;

public sealed class GameAutoLaunchHostedService(
    ICredentialStore credentialStore,
    IGameSessionStore sessionStore,
    IGameLaunchAppService gameLaunch,
    IHostApplicationLifetime lifetime,
    ILogger<GameAutoLaunchHostedService> logger) : IHostedService
{
    private static readonly TimeSpan StopWaitTimeout = TimeSpan.FromSeconds(3);

    private Task? _autoLaunchTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!sessionStore.HasSession && !credentialStore.HasSaved)
            return Task.CompletedTask;

        _autoLaunchTask = RunAutoLaunchAsync(lifetime.ApplicationStopping);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_autoLaunchTask is null)
            return;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(StopWaitTimeout);
            await _autoLaunchTask.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Auto-launch task did not finish within shutdown timeout");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Auto-launch task failed during shutdown");
        }
    }

    private async Task RunAutoLaunchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await AutoLaunchCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Auto-launch failed — open Login to start the game client");
        }
    }

    private async Task AutoLaunchCoreAsync(CancellationToken cancellationToken)
    {
        GameLaunchParameters launch;

        if (sessionStore.Current is not null)
        {
            logger.LogInformation("Auto-launching game client from saved session");
            launch = sessionStore.Current;
        }
        else if (credentialStore.TryLoad(out var credentials))
        {
            logger.LogInformation("Auto-launching game client from saved credentials");
            launch = GameLaunchParameters.FromCredentials(credentials.Username, credentials.Password);
            sessionStore.Save(launch);
        }
        else
        {
            return;
        }

        await gameLaunch.LaunchAndConnectAsync(launch, cancellationToken).ConfigureAwait(false);
    }
}
