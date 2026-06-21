using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

public sealed class GameLauncherService : IGameLauncherService
{
    private readonly FridaGameApi _fridaApi;
    private readonly DarkorbitClientLauncher _clientLauncher;
    private readonly GameClientConnectService _connectService;
    private readonly GameSessionStore _sessionStore;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameLauncherService> _logger;
    private readonly SemaphoreSlim _launchLock = new(1, 1);

    public GameLauncherService(
        FridaGameApi fridaApi,
        DarkorbitClientLauncher clientLauncher,
        GameClientConnectService connectService,
        GameSessionStore sessionStore,
        IOptions<GameApiOptions> options,
        ILogger<GameLauncherService> logger)
    {
        _fridaApi = fridaApi;
        _clientLauncher = clientLauncher;
        _connectService = connectService;
        _sessionStore = sessionStore;
        _options = options.Value;
        _logger = logger;
    }

    public IGameConnection ActiveConnection => _fridaApi;

    public async Task<GameClientConnectResult> LaunchAndConnectAsync(
        GameLaunchParameters launch,
        CancellationToken cancellationToken = default)
    {
        await LaunchAsync(launch, cancellationToken).ConfigureAwait(false);
        return await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LaunchAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default)
    {
        await _launchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_options.BrowserApi == GameApiMode.BackpageOnly)
            {
                _sessionStore.Save(launch);
                _logger.LogInformation("Backpage-only mode — skipping game client launch");
                return;
            }

            _sessionStore.Save(launch);

            if (_clientLauncher.IsRunning)
            {
                _logger.LogInformation("Darkorbit-client already running — skipping spawn");
            }
            else
            {
                _fridaApi.MarkLaunching();
                _clientLauncher.Launch(launch);
                _logger.LogInformation("Darkorbit-client started — load the map, bot connects via Frida bridge");
            }
        }
        finally
        {
            _launchLock.Release();
        }
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_options.BrowserApi != GameApiMode.BackpageOnly)
            _fridaApi.MarkWaitingForGameLoad();

        var result = await _connectService.ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            _fridaApi.MarkFailed(result.Error ?? "Connect failed.");

        return result;
    }

    public void AttachProcess(long pid) => _fridaApi.AttachProcess(pid);
}
