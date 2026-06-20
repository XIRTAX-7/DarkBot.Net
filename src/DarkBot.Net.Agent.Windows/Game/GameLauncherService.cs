using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

public sealed class GameLauncherService
{
    private readonly NativeGameBridge _bridge;
    private readonly NativeLibrarySetup _librarySetup;
    private readonly FridaGameApi _fridaApi;
    private readonly DarkorbitClientLauncher _clientLauncher;
    private readonly GameClientConnectService _connectService;
    private readonly GameSessionStore _sessionStore;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameLauncherService> _logger;
    private readonly SemaphoreSlim _launchLock = new(1, 1);

    public GameLauncherService(
        NativeGameBridge bridge,
        NativeLibrarySetup librarySetup,
        FridaGameApi fridaApi,
        DarkorbitClientLauncher clientLauncher,
        GameClientConnectService connectService,
        GameSessionStore sessionStore,
        IOptions<GameApiOptions> options,
        ILogger<GameLauncherService> logger)
    {
        _bridge = bridge;
        _librarySetup = librarySetup;
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

            // Start Electron immediately — JVM bridge init can take 30+ s and must not block the client window.
            _clientLauncher.Launch(launch);
            _logger.LogInformation("Darkorbit-client started — load the map, then bot will attach via Frida");
        }
        finally
        {
            _launchLock.Release();
        }
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await EnsureEmbeddedBridgeAsync(cancellationToken).ConfigureAwait(false);
        return await _connectService.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public void AttachProcess(long pid) => _fridaApi.AttachProcess(pid);

    private async Task EnsureEmbeddedBridgeAsync(CancellationToken cancellationToken)
    {
        if (_bridge.IsInitialized)
            return;

        await _librarySetup.EnsureLibrariesAsync(cancellationToken).ConfigureAwait(false);
        _librarySetup.PrepareRuntimePath();

        var libDir = NativeBridgePaths.ResolveLibDir(_options.LibPath);
        var workingDir = NativeBridgePaths.ResolveJvmWorkingDirectory(_options.JvmWorkingDirectory, libDir);

        if (!NativeBridgePaths.EnsureDarkBotJarInLib(libDir, _options.DarkBotJarPath, _logger))
        {
            _logger.LogWarning(
                "DarkBot.jar not found — DarkMem JNI may fail. Copy to ./lib/DarkBot.jar for memory reads.");
        }

        var classPath = NativeBridgePaths.BuildBridgeClassPath(
            NativeBridgePaths.ResolveClassesDir(_options.ClassesPath),
            libDir,
            _options.DarkBotJarPath);

        _logger.LogInformation(
            "Initializing DarkMem bridge (lib={LibDir}, user.dir={WorkingDir})",
            libDir,
            workingDir);
        _bridge.Initialize(libDir, classPath, workingDir);
    }
}
