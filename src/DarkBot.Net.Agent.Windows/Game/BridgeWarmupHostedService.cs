using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Pre-downloads native libs and initializes DarkMem bridge while the login UI is shown.</summary>
public sealed class BridgeWarmupHostedService : IHostedService
{
    private readonly GameLauncherService _launcher;
    private readonly GameApiOptions _options;
    private readonly ILogger<BridgeWarmupHostedService> _logger;

    public BridgeWarmupHostedService(
        GameLauncherService launcher,
        IOptions<GameApiOptions> options,
        ILogger<BridgeWarmupHostedService> logger)
    {
        _launcher = launcher;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.BrowserApi == GameApiMode.BackpageOnly)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Pre-warming DarkMem bridge in background");
                await _launcher.WarmupBridgeAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("DarkMem bridge pre-warm finished");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Bridge pre-warm failed — will retry on game connect");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
