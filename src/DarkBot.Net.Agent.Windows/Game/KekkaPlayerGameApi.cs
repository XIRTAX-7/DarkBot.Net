using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>
/// KekkaPlayer launch sequence mirrors Java <c>GameAPIImpl</c>:
/// constructor phase (flash/proxy/min size) → setSize → createWindow(setData + native window on API thread).
/// </summary>
public sealed class KekkaPlayerGameApi : IGameConnection
{
    private readonly NativeGameBridge _bridge;
    private readonly GameApiOptions _options;
    private readonly NativeLibrarySetup _librarySetup;
    private readonly ILogger<KekkaPlayerGameApi> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private GameConnectionPhase _phase = GameConnectionPhase.NotStarted;
    private KekkaPlayerProxyServer? _proxy;
    private GameLaunchParameters? _pendingLaunch;
    private string? _flashOcxPath;
    private int _proxyPort;

    public KekkaPlayerGameApi(
        NativeGameBridge bridge,
        NativeLibrarySetup librarySetup,
        IOptions<GameApiOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<KekkaPlayerGameApi> logger)
    {
        _bridge = bridge;
        _librarySetup = librarySetup;
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public GameApiMode Mode => GameApiMode.KekkaPlayer;

    public GameConnectionPhase Phase => _phase;

    public bool IsLaunched => _phase is GameConnectionPhase.WaitingForGameLoad or GameConnectionPhase.Connected;

    public bool IsValid => _bridge.IsKekkaAvailable && _bridge.Kekka.IsValid;

    public string? LastFailureReason { get; private set; }

    public event Action<GameConnectionPhase>? PhaseChanged;

    /// <summary>
    /// Launch KekkaPlayer via standalone Java process (Java DarkBot algorithm).
    /// Does not embed JVM in the Avalonia process.
    /// </summary>
    public void LaunchViaJavaProcess(GameLaunchParameters launch, KekkaPlayerProcessLauncher processLauncher)
    {
        SetPhase(GameConnectionPhase.Launching);
        LastFailureReason = null;

        _librarySetup.EnsureFlashOcxPresent();
        var flashOcxPath = _librarySetup.ResolveFlashOcxPath();

        _logger.LogInformation(
            "KekkaPlayer launch: instance={Instance}, userId={UserId}, preloader={Preloader}, varsLength={VarsLength}, flash={Flash}",
            launch.InstanceUrl,
            launch.UserId,
            launch.PreloaderUrl,
            FlashVarBuilder.BuildVarsString(launch.FlashParams, _options).Length,
            flashOcxPath);

        var proxyPort = 0;
        if (_options.UseProxy || OperatingSystem.IsWindowsVersionAtLeast(6, 1) == false)
            _logger.LogWarning("Java-process launch: local proxy is not started yet (UseProxy ignored).");

        processLauncher.Launch(launch, flashOcxPath, _options, proxyPort);
        SetPhase(GameConnectionPhase.WaitingForGameLoad);
        _logger.LogInformation(
            "KekkaPlayer Java process started (pid={Pid}). See kekka-launcher-out.log in app folder.",
            processLauncher.LastProcessId);
    }

    public void ConfigureLaunch(GameLaunchParameters launch)
    {
        EnsureBridgeReady();

        if (!_bridge.IsKekkaAvailable)
        {
            var detail = _bridge.LastNativeError;
            var message = string.IsNullOrWhiteSpace(detail)
                ? "KekkaPlayer is not available."
                : $"KekkaPlayer is not available: {detail}";
            _logger.LogError("{Message}", message);
            throw new InvalidOperationException(message);
        }

        SetPhase(GameConnectionPhase.Launching);
        LastFailureReason = null;
        _pendingLaunch = launch;

        // Resolve flash path and proxy on the caller thread; all KekkaPlayer JNI/COM setup runs on the API thread.
        _librarySetup.EnsureFlashOcxPresent();
        _flashOcxPath = _librarySetup.ResolveFlashOcxPath();
        _logger.LogInformation("KekkaPlayer Flash OCX: {Path}", _flashOcxPath);

        _proxyPort = 0;
        if (_options.UseProxy || OperatingSystem.IsWindowsVersionAtLeast(6, 1) == false)
        {
            _proxy?.Dispose();
            _proxy = new KekkaPlayerProxyServer(
                _bridge.Kekka,
                _loggerFactory.CreateLogger<KekkaPlayerProxyServer>());
            _proxy.StartWithoutNativeProxy();
            _proxyPort = _proxy.Port;
        }
    }

    public void CreateWindow()
    {
        EnsureBridgeReady();

        if (_pendingLaunch is null)
            throw new InvalidOperationException("Launch parameters were not configured.");

        var launch = _pendingLaunch;
        var sid = $"dosid={launch.Sid}";
        var vars = FlashVarBuilder.BuildVarsString(launch.FlashParams, _options);

        _logger.LogInformation(
            "KekkaPlayer launch: instance={Instance}, userId={UserId}, preloader={Preloader}, varsLength={VarsLength}",
            launch.InstanceUrl,
            launch.UserId,
            launch.PreloaderUrl,
            vars.Length);

        if (string.IsNullOrWhiteSpace(_flashOcxPath))
            throw new InvalidOperationException("Flash OCX path was not configured.");

        // GameAPIImpl.createWindow(): setData + API thread with createWindow().
        // Flash ActiveX/COM must be configured on the same STA thread as createWindow().
        _bridge.Kekka.LaunchWindow(
            launch.InstanceUrl,
            sid,
            launch.PreloaderUrl,
            vars,
            _flashOcxPath,
            _options.Width,
            _options.Height,
            800,
            600,
            _proxyPort);
        EnsureNoNativeError("launchWindow");

        KekkaLaunchDebugWriter.TryWrite(launch, _flashOcxPath, _options, _proxyPort);

        SetPhase(GameConnectionPhase.WaitingForGameLoad);
        _logger.LogInformation(
            "KekkaPlayer API thread started (flash + setData + createWindow, proxyPort={ProxyPort}). " +
            "Debug: run scripts/run-kekka-launcher.ps1 -PropertiesFile launch.properties after login.",
            _proxyPort);
    }

    public int ReadInt(long address) => UseKekkaMemory().ReadInt(address);

    public long ReadLong(long address) => UseKekkaMemory().ReadLong(address);

    public double ReadDouble(long address) => UseKekkaMemory().ReadDouble(address);

    public long SearchPattern(ReadOnlySpan<byte> pattern) => UseKekkaMemory().QueryBytes(pattern);

    public long SearchClassClosure(Func<long, bool> pattern) => 0;

    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0) =>
        _bridge.Kekka.MoveShip(screenManager, x, y, collectableAddress);

    public void Reload() => _bridge.Kekka.Reload();

    public void HandleRefresh(bool useFakeDailyLogin = true)
    {
        Reload();
        SetPhase(GameConnectionPhase.WaitingForGameLoad);
    }

    public long LastInternetReadTime() =>
        _bridge.IsInitialized ? _bridge.Kekka.LastInternetReadTime() : 0;

    public void ClearCache(string pattern) => _bridge.Kekka.ClearCache(pattern);

    public IReadOnlyList<GameProcessInfo> GetProcesses() => _bridge.GetProcesses();

    public void OpenProcess(long pid) => _bridge.OpenProcess(pid);

    internal void EnsureBridgeReady()
    {
        if (!_bridge.IsInitialized)
            throw new InvalidOperationException("Native bridge is not initialized.");
    }

    private NativeKekkaBridge UseKekkaMemory() => _bridge.Kekka;

    private void SetPhase(GameConnectionPhase phase)
    {
        if (_phase == phase)
            return;

        _phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    public void MarkConnected() => SetPhase(GameConnectionPhase.Connected);

    public void MarkFailed() => FailLaunch("Game launch failed.");

    public void FailLaunch(string reason)
    {
        LastFailureReason = reason;
        _logger.LogError("KekkaPlayer launch failed: {Reason}", reason);
        SetPhase(GameConnectionPhase.Failed);
    }

    private void EnsureNoNativeError(string operation)
    {
        var error = _bridge.LastNativeError;
        if (string.IsNullOrWhiteSpace(error))
            return;

        var message = $"KekkaPlayer.{operation} failed: {error}";
        _logger.LogError("{Message}", message);
        throw new InvalidOperationException(message);
    }
}

