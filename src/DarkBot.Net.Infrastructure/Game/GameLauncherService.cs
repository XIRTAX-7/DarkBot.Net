using DarkBot.Net.Core.Models.Game;

using DarkBot.Net.Core.Interfaces.Game;

using DarkBot.Net.Core.Options;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;



namespace DarkBot.Net.Infrastructure.Game;



public sealed class GameLauncherService : IGameLauncherService

{

    private readonly IGameConnection _gameConnection;

    private readonly FridaGameApi _legacyFridaApi;

    private readonly UnityFridaGameApi _unityFridaApi;

    private readonly DarkorbitClientLauncher _clientLauncher;

    private readonly UnityGameLauncher _unityLauncher;

    private readonly GameClientConnectService _connectService;

    private readonly GameSessionStore _sessionStore;

    private readonly GameApiOptions _options;

    private readonly ILogger<GameLauncherService> _logger;

    private readonly SemaphoreSlim _launchLock = new(1, 1);



    public GameLauncherService(

        IGameConnection gameConnection,

        FridaGameApi legacyFridaApi,

        UnityFridaGameApi unityFridaApi,

        DarkorbitClientLauncher clientLauncher,

        UnityGameLauncher unityLauncher,

        GameClientConnectService connectService,

        GameSessionStore sessionStore,

        IOptions<GameApiOptions> options,

        ILogger<GameLauncherService> logger)

    {

        _gameConnection = gameConnection;

        _legacyFridaApi = legacyFridaApi;

        _unityFridaApi = unityFridaApi;

        _clientLauncher = clientLauncher;

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

            if (_options.BrowserApi == GameApiMode.BackpageOnly)

            {

                _sessionStore.Save(launch);

                _logger.LogInformation("Backpage-only mode — skipping game client launch");

                return;

            }



            _sessionStore.Save(launch);



            if (_options.BrowserApi == GameApiMode.UnityClient)

            {

                if (_unityLauncher.IsRunning)

                {

                    _logger.LogInformation("Unity game already running — skipping spawn");

                }

                else

                {

                    _unityFridaApi.MarkLaunching();

                    _unityLauncher.Launch(launch);

                    _logger.LogInformation(

                        "Unity game started from {InstallPath} — waiting for map load and Frida attach",

                        _options.UnityGameInstallPath);

                }



                return;

            }



            if (_clientLauncher.IsRunning)

            {

                _logger.LogInformation("Darkorbit-client already running — skipping spawn");

            }

            else

            {

                _legacyFridaApi.MarkLaunching();

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

        {

            if (_options.BrowserApi == GameApiMode.UnityClient)

                _unityFridaApi.MarkWaitingForGameLoad();

            else

                _legacyFridaApi.MarkWaitingForGameLoad();

        }



        var result = await _connectService.ConnectAsync(cancellationToken).ConfigureAwait(false);

        if (!result.Success)

        {

            if (_options.BrowserApi == GameApiMode.UnityClient)

                _unityFridaApi.MarkFailed(result.Error ?? "Connect failed.");

            else

                _legacyFridaApi.MarkFailed(result.Error ?? "Connect failed.");

        }



        return result;

    }



    public async Task<GameClientConnectResult> RestartClientAsync(

        GameLaunchParameters launch,

        CancellationToken cancellationToken = default)

    {

        await _launchLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try

        {

            if (_options.BrowserApi == GameApiMode.BackpageOnly)

                return GameClientConnectResult.Fail("Backpage-only mode");



            _sessionStore.Save(launch);

            if (_options.BrowserApi == GameApiMode.UnityClient)

                _unityFridaApi.ResetConnectionState();

            else

                _legacyFridaApi.ResetConnectionState();



            if (_options.BrowserApi == GameApiMode.UnityClient)

            {

                if (_unityLauncher.IsRunning)

                    await _unityLauncher.StopAsync(cancellationToken).ConfigureAwait(false);



                _unityFridaApi.MarkLaunching();

                _unityLauncher.Launch(launch);

                _logger.LogInformation("Unity game restarted from {InstallPath}", _options.UnityGameInstallPath);

            }

            else

            {

                if (_clientLauncher.IsRunning)

                    await _clientLauncher.StopAsync(cancellationToken).ConfigureAwait(false);



                _legacyFridaApi.MarkLaunching();

                _clientLauncher.Launch(launch);

                _logger.LogInformation("Darkorbit-client restarted — waiting for map load and Frida bridge");

            }

        }

        finally

        {

            _launchLock.Release();

        }



        return await ConnectAsync(cancellationToken).ConfigureAwait(false);

    }



    public void AttachProcess(long pid)

    {

        if (_options.BrowserApi == GameApiMode.UnityClient)

            _unityFridaApi.AttachProcess(pid);

        else

            _legacyFridaApi.AttachProcess(pid);

    }

}


