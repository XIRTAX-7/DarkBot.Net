using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

/// <summary>Подключение к Unity-клиенту через FridaCLR.</summary>
public sealed class GameClientConnectService
{
    private readonly UnityFridaGameApi _unityFrida;
    private readonly UnityFridaSession _unitySession;
    private readonly UnitySessionBootstrapStore _bootstrapStore;
    private readonly UnityProcessFinder _processFinder;
    private readonly UnityGameLauncher _unityLauncher;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientConnectService> _logger;

    public GameClientConnectService(
        UnityFridaGameApi unityFrida,
        UnityFridaSession unitySession,
        UnitySessionBootstrapStore bootstrapStore,
        UnityProcessFinder processFinder,
        UnityGameLauncher unityLauncher,
        IOptions<GameApiOptions> options,
        ILogger<GameClientConnectService> logger)
    {
        _unityFrida = unityFrida;
        _unitySession = unitySession;
        _bootstrapStore = bootstrapStore;
        _processFinder = processFinder;
        _unityLauncher = unityLauncher;
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
}
