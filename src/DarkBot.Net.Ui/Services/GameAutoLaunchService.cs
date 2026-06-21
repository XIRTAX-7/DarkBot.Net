using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Login;
using DarkBot.Net.Ui.Game;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Ui.Services;

public sealed class GameAutoLaunchService : IHostedService
{
    private readonly IBackpageApi _backpage;
    private readonly GameSessionStore _sessionStore;
    private readonly GameLaunchOrchestrator _orchestrator;
    private readonly LoginService _loginService;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameAutoLaunchService> _logger;

    public GameAutoLaunchService(
        IBackpageApi backpage,
        GameSessionStore sessionStore,
        GameLaunchOrchestrator orchestrator,
        LoginService loginService,
        IOptions<GameApiOptions> options,
        ILogger<GameAutoLaunchService> logger)
    {
        _backpage = backpage;
        _sessionStore = sessionStore;
        _orchestrator = orchestrator;
        _loginService = loginService;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_backpage.IsInstanceValid() || _options.BrowserApi == GameApiMode.BackpageOnly)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                await AutoLaunchCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-launch failed — open Login to start the game client");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    private async Task AutoLaunchCoreAsync(CancellationToken cancellationToken)
    {
        GameLaunchParameters launch;

        if (_sessionStore.Current is not null)
        {
            _logger.LogInformation("Auto-launching Darkorbit-client from saved session");
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
            var loginData = await _loginService.LoginWithSidAsync(server, _backpage.Sid, cancellationToken)
                .ConfigureAwait(false);
            launch = GameLaunchMapper.ToLaunchParameters(loginData);
            _sessionStore.Save(launch);
        }

        await _orchestrator.LaunchAndConnectAsync(launch, cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
