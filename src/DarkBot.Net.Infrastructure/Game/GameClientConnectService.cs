using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Подключение к игре: Unity (FridaCLR) или legacy Electron bridge.</summary>
public sealed class GameClientConnectService
{
    private readonly FridaGameApi _legacyFrida;
    private readonly UnityFridaGameApi _unityFrida;
    private readonly UnityFridaSession _unitySession;
    private readonly UnitySessionBootstrapStore _bootstrapStore;
    private readonly UnityProcessFinder _processFinder;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly ElectronControlClient _control;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientConnectService> _logger;

    public GameClientConnectService(
        FridaGameApi legacyFrida,
        UnityFridaGameApi unityFrida,
        UnityFridaSession unitySession,
        UnitySessionBootstrapStore bootstrapStore,
        UnityProcessFinder processFinder,
        UnityGameLauncher unityLauncher,
        ElectronControlClient control,
        IOptions<GameApiOptions> options,
        ILogger<GameClientConnectService> logger)
    {
        _legacyFrida = legacyFrida;
        _unityFrida = unityFrida;
        _unitySession = unitySession;
        _bootstrapStore = bootstrapStore;
        _processFinder = processFinder;
        _unityLauncher = unityLauncher;
        _control = control;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default) =>
        _options.BrowserApi == GameApiMode.UnityClient
            ? await ConnectUnityAsync(cancellationToken).ConfigureAwait(false)
            : await ConnectLegacyAsync(cancellationToken).ConfigureAwait(false);

    private async Task<GameClientConnectResult> ConnectUnityAsync(CancellationToken cancellationToken)
    {
        var pid = await _unityLauncher.WaitForProcessIdAsync(cancellationToken).ConfigureAwait(false);
        if (pid == 0)
            pid = _processFinder.FindRunningProcessId();

        if (pid == 0)
        {
            return GameClientConnectResult.Fail(
                $"Process {_options.UnityProcessName} not found in {_options.UnityGameInstallPath}. Launch failed or timed out.");
        }

        var attachDelaySec = _options.UnityAuthViaHook
            ? _options.UnityEarlyAttachDelaySec
            : _options.UnityFridaAttachDelaySec;

        if (attachDelaySec > 0)
        {
            _logger.LogInformation(
                "Waiting {DelaySec}s before Frida attach ({Mode})",
                attachDelaySec,
                _options.UnityAuthViaHook ? "WebView autologin bootstrap" : "movement hooks");
            await Task.Delay(TimeSpan.FromSeconds(attachDelaySec), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await _unityFrida.AttachProcessAsync(pid, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity Frida attach failed for pid={Pid}", pid);
            return GameClientConnectResult.Fail(ex.Message);
        }

        _logger.LogInformation("Unity game client pid={Pid} (FridaCLR attach)", pid);

        if (_options.UnityAuthViaHook && _bootstrapStore.TryTake(out var session))
        {
            try
            {
                await _unitySession.BootstrapSessionAsync(session, cancellationToken).ConfigureAwait(false);
                await _unitySession.WaitForBootstrapPipelineAsync(
                    requireSessionInject: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unity WebView autologin bootstrap failed");
                return GameClientConnectResult.Fail(ex.Message);
            }
        }

        var ready = await _unityFrida.WaitForReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!ready)
        {
            return GameClientConnectResult.Fail(
                "Unity bridge agent not ready. Stay on the map and retry.");
        }

        return GameClientConnectResult.Ok(pid);
    }

    private async Task<GameClientConnectResult> ConnectLegacyAsync(CancellationToken cancellationToken)
    {
        var pepperPid = await WaitForPepperPidAsync(cancellationToken).ConfigureAwait(false);
        if (pepperPid == 0)
        {
            return GameClientConnectResult.Fail(
                "Pepper Flash process not found. Open the game map in Darkorbit-client.");
        }

        _legacyFrida.AttachProcess(pepperPid);
        _logger.LogInformation("Game client Pepper pid={Pid} (Frida-only attach)", pepperPid);

        var fridaReady = await _legacyFrida.WaitForReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!fridaReady)
        {
            return GameClientConnectResult.Fail(
                $"Frida game API not ready on :{_options.FridaApiPort}. Stay on the map and retry.");
        }

        return GameClientConnectResult.Ok(pepperPid);
    }

    private async Task<int> WaitForPepperPidAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.ClientConnectTimeoutSec);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var controlPid = await TryGetPepperPidFromControlAsync(cancellationToken).ConfigureAwait(false);
            if (controlPid > 0)
            {
                _logger.LogInformation("Detected Pepper pid={Pid} via control WS", controlPid);
                return controlPid;
            }

            await Task.Delay(_options.ConnectPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private async Task<int> TryGetPepperPidFromControlAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _control.TryConnectAsync(cancellationToken).ConfigureAwait(false))
                return 0;

            return await _control.GetPepperPidAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Control WS getPid failed");
            return 0;
        }
    }
}
