using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

/// <summary>
/// Подключение к Unity-клиенту через FridaCLR.
/// Один attach на весь жизненный цикл бота: auth → ангар → карта → gameplay.
/// Detach только при выходе игры, рестарте клиента или остановке бота.
/// </summary>
public sealed class GameClientConnectService
{
    private readonly UnityFridaGameApi _unityFrida;
    private readonly UnityFridaSession _unitySession;
    private readonly UnitySessionBootstrapStore _bootstrapStore;
    private readonly UnityProcessFinder _processFinder;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly GameSessionStore _sessionStore;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientConnectService> _logger;

    public GameClientConnectService(
        UnityFridaGameApi unityFrida,
        UnityFridaSession unitySession,
        UnitySessionBootstrapStore bootstrapStore,
        UnityProcessFinder processFinder,
        UnityGameLauncher unityLauncher,
        GameSessionStore sessionStore,
        IOptions<GameApiOptions> options,
        ILogger<GameClientConnectService> logger)
    {
        _unityFrida = unityFrida;
        _unitySession = unitySession;
        _bootstrapStore = bootstrapStore;
        _processFinder = processFinder;
        _unityLauncher = unityLauncher;
        _sessionStore = sessionStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var pid = await _unityLauncher.WaitForProcessIdAsync(cancellationToken).ConfigureAwait(false);
        if (pid == 0)
            pid = _processFinder.FindRunningProcessId();

        if (pid == 0)
        {
            return GameClientConnectResult.Fail(
                $"Process {_options.UnityProcessName} not found in {_options.UnityGameInstallPath}. Launch failed or timed out.");
        }

        if (_unitySession.IsAttached && _unitySession.AttachedPid == pid && _unityFrida.IsValid)
        {
            _logger.LogInformation("Unity Frida session already active for pid={Pid}", pid);
            return GameClientConnectResult.Ok(pid);
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
            await AttachOnceAsync(pid, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity Frida attach failed for pid={Pid}", pid);
            return GameClientConnectResult.Fail(ex.Message);
        }

        if (_options.UnityAuthViaHook)
        {
            if (!TryResolveBootstrapSession(out var session))
            {
                _logger.LogWarning(
                    "Unity WebView autologin skipped — no credentials staged (launch bot with profile login first)");
            }
            else
            {
                try
                {
                    await RunAuthAndEnterMapPipelineAsync(session, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unity auth / enter-map pipeline failed");
                    return GameClientConnectResult.Fail(ex.Message);
                }
            }
        }

        var ready = await WaitForGameplayReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!ready)
        {
            return GameClientConnectResult.Fail(
                "Unity bridge agent not ready. Stay on the map and retry.");
        }

        _unityFrida.MarkGameplayReady();
        return GameClientConnectResult.Ok(pid);
    }

    private async Task AttachOnceAsync(int pid, CancellationToken cancellationToken)
    {
        await _unityFrida.AttachProcessAsync(pid, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Unity game client pid={Pid} — single Frida session (auth + map share one agent)",
            pid);
    }

    private bool TryResolveBootstrapSession(out UnityWebGlSession session)
    {
        if (_bootstrapStore.TryTake(out session))
            return true;

        var launch = _sessionStore.Current;
        if (launch is null
            || string.IsNullOrWhiteSpace(launch.Username)
            || string.IsNullOrWhiteSpace(launch.Password))
        {
            session = new UnityWebGlSession();
            return false;
        }

        session = new UnityWebGlSession(launch.Username, launch.Password);
        _logger.LogInformation(
            "Unity WebView autologin credentials restored from active launch session ({Username})",
            launch.Username);
        return true;
    }

    private async Task RunAuthAndEnterMapPipelineAsync(
        UnityWebGlSession session,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unity bridge: auth phase (bootstrap + WebView autologin)");
        await _unitySession.BootstrapSessionAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitySession.WaitForBootstrapPipelineAsync(
            requireSessionInject: true,
            cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Unity bridge: auth phase complete — enter-map handled in same session");
    }

    private Task<bool> WaitForGameplayReadyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unity bridge: gameplay phase — waiting for movement hooks");
        return _unityFrida.WaitForReadyAsync(cancellationToken);
    }
}
