using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using DarkBot.Net.Presentation.Game;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Presentation.Services;

public sealed class GameAutoLaunchService : IHostedService
{
    private static readonly TimeSpan StopWaitTimeout = TimeSpan.FromSeconds(3);

    private readonly IBackpageApi _backpage;
    private readonly GameSessionStore _sessionStore;
    private readonly IGameLaunchAppService _gameLaunch;
    private readonly ILoginAppService _loginApp;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameAutoLaunchService> _logger;
    private Task? _autoLaunchTask;

    public GameAutoLaunchService(
        IBackpageApi backpage,
        GameSessionStore sessionStore,
        IGameLaunchAppService gameLaunch,
        ILoginAppService loginApp,
        IHostApplicationLifetime lifetime,
        IOptions<GameApiOptions> options,
        ILogger<GameAutoLaunchService> logger)
    {
        _backpage = backpage;
        _sessionStore = sessionStore;
        _gameLaunch = gameLaunch;
        _loginApp = loginApp;
        _lifetime = lifetime;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_backpage.IsInstanceValid() || _options.BrowserApi == GameApiMode.BackpageOnly)
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
        else if (string.IsNullOrWhiteSpace(_backpage.Sid) || _backpage.InstanceUri is null)
        {
            return;
        }
        else
        {
            _logger.LogInformation("Auto-launching game by refreshing preloader for existing SID session");
            var host = _backpage.InstanceUri.Host;
            var server = host.Replace(".darkorbit.com", "", StringComparison.OrdinalIgnoreCase);
            var loginData = await _loginApp.LoginWithSidAsync(server, _backpage.Sid, cancellationToken)
                .ConfigureAwait(false);
            launch = GameLaunchMapper.ToLaunchParameters(loginData);
            _sessionStore.Save(launch);
        }

        await _gameLaunch.LaunchAndConnectAsync(launch, cancellationToken).ConfigureAwait(false);
    }
}
