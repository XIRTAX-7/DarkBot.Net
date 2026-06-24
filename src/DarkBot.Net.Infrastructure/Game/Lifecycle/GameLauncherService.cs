using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

public sealed class GameLauncherService : IGameLauncherService
{
    private readonly IGameConnection _gameConnection;
    private readonly UnityFridaGameApi _unityFridaApi;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly GameClientConnectService _connectService;
    private readonly GameSessionStore _sessionStore;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameLauncherService> _logger;
    private readonly SemaphoreSlim _launchLock = new(1, 1);

    public GameLauncherService(
        IGameConnection gameConnection,
        UnityFridaGameApi unityFridaApi,
        UnityGameLauncher unityLauncher,
        GameClientConnectService connectService,
        GameSessionStore sessionStore,
        IOptions<GameApiOptions> options,
        ILogger<GameLauncherService> logger)
    {
        _gameConnection = gameConnection;
        _unityFridaApi = unityFridaApi;
        _unityLauncher = unityLauncher;
        _connectService = connectService;
        _sessionStore = sessionStore;
        _options = options.Value;
        _logger = logger;
    }

    public IGameConnection ActiveConnection => _gameConnection;

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
            _sessionStore.Save(launch);

            if (_unityLauncher.IsRunning)
            {
                _logger.LogInformation("Unity game already running — skipping spawn");
                return;
            }

            _unityFridaApi.MarkLaunching();
            _unityLauncher.Launch(launch);
            _logger.LogInformation(
                "Unity game started from {InstallPath} — waiting for map load and Frida attach",
                _options.UnityGameInstallPath);
        }
        finally
        {
            _launchLock.Release();
        }
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _unityFridaApi.MarkWaitingForGameLoad();

        var result = await _connectService.ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            _unityFridaApi.MarkFailed(result.Error ?? "Connect failed.");

        return result;
    }

    public async Task<GameClientConnectResult> RestartClientAsync(
        GameLaunchParameters launch,
        CancellationToken cancellationToken = default)
    {
        await _launchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _sessionStore.Save(launch);
            _unityFridaApi.ResetConnectionState();

            if (_unityLauncher.IsRunning)
                await _unityLauncher.StopAsync(cancellationToken).ConfigureAwait(false);

            _unityFridaApi.MarkLaunching();
            _unityLauncher.Launch(launch);
            _logger.LogInformation("Unity game restarted from {InstallPath}", _options.UnityGameInstallPath);
        }
        finally
        {
            _launchLock.Release();
        }

        return await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public void AttachProcess(long pid) => _unityFridaApi.AttachProcess(pid);
}
