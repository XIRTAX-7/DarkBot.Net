using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Presentation.Services.Game;

public sealed class GameAutoLaunchService : IHostedService
{
    private static readonly TimeSpan StopWaitTimeout = TimeSpan.FromSeconds(3);

    private readonly ICredentialStore _credentialStore;
    private readonly GameSessionStore _sessionStore;
    private readonly IGameLaunchAppService _gameLaunch;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<GameAutoLaunchService> _logger;
    private Task? _autoLaunchTask;

    public GameAutoLaunchService(
        ICredentialStore credentialStore,
        GameSessionStore sessionStore,
        IGameLaunchAppService gameLaunch,
        IHostApplicationLifetime lifetime,
        ILogger<GameAutoLaunchService> logger)
    {
        _credentialStore = credentialStore;
        _sessionStore = sessionStore;
        _gameLaunch = gameLaunch;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_sessionStore.HasSession && !_credentialStore.HasSaved)
            return Task.CompletedTask;

        _autoLaunchTask = RunAutoLaunchAsync(_lifetime.ApplicationStopping);
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
            _logger.LogDebug("Auto-launch task did not finish within shutdown timeout");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Auto-launch task failed during shutdown");
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
            _logger.LogWarning(ex, "Auto-launch failed — open Login to start the game client");
        }
    }

    private async Task AutoLaunchCoreAsync(CancellationToken cancellationToken)
    {
        GameLaunchParameters launch;

        if (_sessionStore.Current is not null)
        {
            _logger.LogInformation("Auto-launching game client from saved session");
            launch = _sessionStore.Current;
        }
        else if (_credentialStore.TryLoad(out var credentials))
        {
            _logger.LogInformation("Auto-launching game client from saved credentials");
            launch = GameLaunchParameters.FromCredentials(credentials.Username, credentials.Password);
            _sessionStore.Save(launch);
        }
        else
        {
            return;
        }

        await _gameLaunch.LaunchAndConnectAsync(launch, cancellationToken).ConfigureAwait(false);
    }
}
