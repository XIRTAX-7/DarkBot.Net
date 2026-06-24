using System.Text.Json;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using Frida;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>FridaCLR-сессия: attach к DarkOrbit.exe и загрузка unity_bridge_agent.js.</summary>
public sealed class UnityFridaSession : IDisposable
{
    private readonly GameApiOptions _options;
    private readonly ILogger<UnityFridaSession> _logger;
    private readonly object _gate = new();

    private DeviceManager? _deviceManager;
    private Device? _device;
    private Frida.Session? _session;
    private Script? _script;
    private FridaRpcClient? _rpc;
    private int _attachedPid;

    public UnityFridaSession(IOptions<GameApiOptions> options, ILogger<UnityFridaSession> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsAttached => _attachedPid != 0;

    public int AttachedPid => _attachedPid;

    public event Action<JsonElement>? AgentEvent;

    public async Task AttachAndLoadAgentAsync(int pid, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_attachedPid == pid && _rpc is not null)
                return;

            DetachCore();
        }

        var agentPath = UnityBridgeAgentPaths.Resolve(_options.UnityBridgeAgentPath);
        var source = await File.ReadAllTextAsync(agentPath, cancellationToken).ConfigureAwait(false);

        _deviceManager = new DeviceManager();
        _device = _deviceManager.EnumerateDevices()
            .FirstOrDefault(static d => d.Type == DeviceType.Local)
            ?? throw new InvalidOperationException("Local Frida device not found.");

        _session = _device.Attach((uint)pid);
        _session.Detached += OnSessionDetached;

        _script = _session.CreateScript(source);
        _rpc = new FridaRpcClient(_script, _logger);
        _script.Message += OnAgentMessage;
        _script.Load();

        _attachedPid = pid;

        await WaitForAgentReadyAsync(cancellationToken).ConfigureAwait(false);

        var agentStatus = UnityBridgeStatusMapper.ParseStatusJson(
            await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));
        _logger.LogInformation(
            "Unity Frida agent loaded from {AgentPath} into pid={Pid} (agentVersion={AgentVersion}, schemaVersion={SchemaVersion})",
            agentPath,
            pid,
            agentStatus?.AgentVersion ?? "unknown",
            agentStatus?.SchemaVersion);
    }

    public async Task<string?> GetStatusJsonAsync(CancellationToken cancellationToken = default)
    {
        var rpc = RequireRpc();
        return await rpc.CallStringAsync("getStatus", cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        var rpc = RequireRpc();
        return await rpc.CallStringAsync("moveTo", [x, y], cancellationToken).ConfigureAwait(false);
    }

    public async Task BootstrapSessionAsync(UnityWebGlSession session, CancellationToken cancellationToken = default)
    {
        // Smoke-тест ждёт 1.5 с после load() перед bootstrapSession — даём хукам стабилизироваться.
        await Task.Delay(1500, cancellationToken).ConfigureAwait(false);

        var rpc = RequireRpc();
        var result = await rpc.CallStringAsync(
            "bootstrapSession",
            ["", "", session.Username ?? "", session.Password ?? ""],
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Unity bootstrapSession RPC: {Result}", result);
    }

    public async Task RefreshSessionAsync(UnityWebGlSession session, CancellationToken cancellationToken = default)
    {
        var rpc = RequireRpc();
        var result = await rpc.CallStringAsync(
            "refreshSession",
            ["", "", session.Username ?? "", session.Password ?? ""],
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Unity refreshSession RPC: {Result}", result);
    }

    public async Task WaitForBootstrapPipelineAsync(
        bool requireSessionInject,
        CancellationToken cancellationToken = default)
    {
        // Smoke успешен при clientUpdateComplete=false — не блокируем auth/map на Addressables.
        var updateWaitSec = Math.Min(30, _options.UnityClientUpdateTimeoutSec);
        var updateDeadline = DateTime.UtcNow.AddSeconds(updateWaitSec);
        while (DateTime.UtcNow < updateDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureAttached();

            var status = UnityBridgeStatusMapper.ParseStatusJson(
                await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));

            if (IsBootstrapPastClientUpdate(status))
                break;

            await Task.Delay(_options.FridaReadyPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        EnsureAttached();
        var statusAfterUpdate = UnityBridgeStatusMapper.ParseStatusJson(
            await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));
        if (statusAfterUpdate?.ClientUpdateComplete == true)
            _logger.LogInformation("Unity client update complete (Addressables)");
        else if (IsBootstrapPastClientUpdate(statusAfterUpdate))
            _logger.LogInformation(
                "Unity client update skipped — pipeline already past login " +
                "(sessionInjected={SessionInjected}, mapStart={MapStart}, getPost={GetPost})",
                statusAfterUpdate?.SessionInjected,
                statusAfterUpdate?.MapStartComplete,
                statusAfterUpdate?.GetPostSeen);

        if (!requireSessionInject)
            return;

        var injectDeadline = DateTime.UtcNow.AddSeconds(_options.UnitySessionInjectTimeoutSec);
        var sessionReady = false;
        while (DateTime.UtcNow < injectDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureAttached();

            var status = UnityBridgeStatusMapper.ParseStatusJson(
                await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));

            if (status?.SessionInjected == true)
            {
                sessionReady = true;
                _logger.LogInformation("Unity session injected via hook");
                break;
            }

            if (status?.MapStartComplete == true)
            {
                sessionReady = true;
                _logger.LogInformation(
                    "Unity session pipeline: already on map (launchShowStartAt={LaunchShowStartAt})",
                    status.LaunchShowStartAt);
                break;
            }

            await Task.Delay(_options.FridaReadyPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        if (!sessionReady)
        {
            var lastStatus = UnityBridgeStatusMapper.ParseStatusJson(
                await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));
            throw new TimeoutException(
                $"Unity session inject did not complete within {_options.UnitySessionInjectTimeoutSec}s " +
                $"(webLoginOpened={lastStatus?.WebLoginOpened}, getPostSeen={lastStatus?.GetPostSeen}, " +
                $"mapStartComplete={lastStatus?.MapStartComplete}, launchShowStartAt={lastStatus?.LaunchShowStartAt}, " +
                $"clientUpdateComplete={lastStatus?.ClientUpdateComplete}).");
        }

        var mapDeadline = DateTime.UtcNow.AddSeconds(_options.UnitySessionInjectTimeoutSec);
        while (DateTime.UtcNow < mapDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureAttached();

            var status = UnityBridgeStatusMapper.ParseStatusJson(
                await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));

            if (status?.Ready == true || status?.MovementHooksReady == true)
            {
                _logger.LogInformation("Unity map ready (movement hooks active)");
                return;
            }

            if (status?.MapStartComplete == true)
            {
                _logger.LogInformation("Unity map loading started (waiting for movement hooks)");
            }

            await Task.Delay(_options.FridaReadyPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        var mapLastStatus = UnityBridgeStatusMapper.ParseStatusJson(
            await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false));
        if (mapLastStatus?.Ready == true || mapLastStatus?.MovementHooksReady == true)
            return;

        throw new TimeoutException(
            $"Unity map enter did not complete within {_options.UnitySessionInjectTimeoutSec}s " +
            $"(sessionInjected={mapLastStatus?.SessionInjected}, mapStartComplete={mapLastStatus?.MapStartComplete}, " +
            $"movementHooksReady={mapLastStatus?.MovementHooksReady}, ready={mapLastStatus?.Ready}).");
    }

    private static bool IsBootstrapPastClientUpdate(UnityBridgeAgentStatus? status) =>
        status?.ClientUpdateComplete == true
        || status?.SessionInjected == true
        || status?.MapStartComplete == true
        || status?.GetPostSeen == true
        || status?.HangarDataReadyAt > 0
        || status?.LaunchShowStarted == true;

    private void EnsureAttached()
    {
        if (_attachedPid == 0 || _rpc is null)
            throw new InvalidOperationException("Unity Frida session is not attached — game process may have exited.");
    }

    public async Task StopAgentAsync(CancellationToken cancellationToken = default)
    {
        var rpc = RequireRpc();
        await rpc.CallRawAsync("stop", cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public void Detach()
    {
        lock (_gate)
            DetachCore();
    }

    private FridaRpcClient RequireRpc() =>
        _rpc ?? throw new InvalidOperationException("Unity Frida session is not attached.");

    private async Task WaitForAgentReadyAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.FridaReadyTimeoutSec);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var statusJson = await GetStatusJsonAsync(cancellationToken).ConfigureAwait(false);
            var status = UnityBridgeStatusMapper.ParseStatusJson(statusJson);
            if (status?.BootstrapHooksReady == true || status?.Ready == true)
                return;

            await Task.Delay(_options.FridaReadyPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Unity bridge agent did not become ready within {_options.FridaReadyTimeoutSec}s.");
    }

    private void OnAgentMessage(object? sender, ScriptMessageEventArgs e)
    {
        if (FridaRpcClient.TryParseSendPayload(e.Message, out var payload)
            && payload.ValueKind == JsonValueKind.Array
            && payload.GetArrayLength() > 0
            && payload[0].GetString() == "frida:rpc")
        {
            _rpc?.HandleIncomingMessage(e.Message);
            return;
        }

        if (FridaRpcClient.TryParseSendPayload(e.Message, out payload)
            && payload.ValueKind == JsonValueKind.Object)
        {
            LogBootstrapAgentEvent(payload);
            AgentEvent?.Invoke(payload);
        }
    }

    private void LogBootstrapAgentEvent(JsonElement payload)
    {
        if (!payload.TryGetProperty("type", out var typeProp))
            return;

        var type = typeProp.GetString();
        if (type is null)
            return;

        switch (type)
        {
            case "agent_loaded":
                _logger.LogInformation(
                    "Unity bootstrap: agent loaded version={Version}",
                    payload.TryGetProperty("agentVersion", out var version) ? version.GetString() : "unknown");
                break;
            case "bootstrap_hooks_ready":
                _logger.LogInformation("Unity bootstrap: hooks ready");
                break;
            case "game_loading_manager_found":
                _logger.LogInformation(
                    "Unity bootstrap: GameLoadingManager found source={Source}",
                    payload.TryGetProperty("source", out var source) ? source.GetString() : "unknown");
                break;
            case "open_web_login":
                _logger.LogInformation(
                    "Unity bootstrap: OpenWebLogin url={Url}",
                    payload.TryGetProperty("url", out var url) ? url.GetString() : null);
                break;
            case "get_post":
                _logger.LogInformation(
                    "Unity bootstrap: GetPost url={Url}",
                    payload.TryGetProperty("url", out var postUrl) ? postUrl.GetString() : null);
                break;
            case "update_web_data_seen":
                _logger.LogInformation(
                    "Unity bootstrap: UpdateWebData length={Length}",
                    payload.TryGetProperty("length", out var len) ? len.GetInt32() : 0);
                break;
            case "session_injected":
                _logger.LogInformation(
                    "Unity bootstrap: session injected reason={Reason}",
                    payload.TryGetProperty("reason", out var reason) ? reason.GetString() : "unknown");
                break;
            case "client_update_complete":
                _logger.LogInformation("Unity bootstrap: Addressables update complete");
                break;
            case "bootstrap_error":
                _logger.LogWarning(
                    "Unity bootstrap error: {Message}",
                    payload.TryGetProperty("message", out var msg) ? msg.GetString() : "unknown");
                break;
            case "web_autologin_sent":
                _logger.LogInformation("Unity bootstrap: WebView autologin script sent");
                break;
            case "web_autologin_scheduled":
                _logger.LogInformation(
                    "Unity bootstrap: WebView autologin scheduled phase={Phase}",
                    payload.TryGetProperty("phase", out var phase) ? phase.GetString() : "unknown");
                break;
            case "web_autologin_skip":
                _logger.LogWarning(
                    "Unity bootstrap: WebView autologin skipped ({Reason})",
                    payload.TryGetProperty("reason", out var skipReason) ? skipReason.GetString() : "unknown");
                break;
            case "web_autologin_error":
                _logger.LogWarning(
                    "Unity bootstrap: WebView autologin error: {Message}",
                    payload.TryGetProperty("message", out var autoErr) ? autoErr.GetString() : "unknown");
                break;
            case "auto_enter_map_scheduled":
                _logger.LogInformation(
                    "Unity bootstrap: auto enter map scheduled phase={Phase}",
                    payload.TryGetProperty("phase", out var enterPhase) ? enterPhase.GetString() : "unknown");
                break;
            case "auto_enter_map_poller_started":
                _logger.LogInformation("Unity bootstrap: auto enter map poller started");
                break;
            case "main_menu_loading_ui":
                _logger.LogInformation("Unity bootstrap: main menu LoadingUIShow after session");
                break;
            case "main_menu_hangar_ready":
                _logger.LogInformation(
                    "Unity bootstrap: main menu hangar data ready reason={Reason}",
                    payload.TryGetProperty("reason", out var hangarReason) ? hangarReason.GetString() : "hook");
                break;
            case "main_menu_launch_show_init":
                _logger.LogInformation("Unity bootstrap: LaunchShow.Init");
                break;
            case "main_menu_launch_show_pre_init":
                _logger.LogInformation("Unity bootstrap: LaunchShow.PreInit");
                break;
            case "main_menu_launch_show_start":
                _logger.LogInformation("Unity bootstrap: LaunchShow.Start (main menu ready)");
                break;
            case "main_menu_launch_show_start_complete":
                _logger.LogInformation(
                    "Unity bootstrap: LaunchShow.Start completed btnStart={BtnStart} autoStart={AutoStart}",
                    payload.TryGetProperty("btnStart", out var btnAtComplete) ? btnAtComplete.GetString() : null,
                    payload.TryGetProperty("autoStartEnabled", out var autoStartProp) && autoStartProp.GetBoolean());
                break;
            case "natural_start_button_press":
                _logger.LogInformation(
                    "Unity bootstrap: game pressed START button={Button} after {Ms}ms source={Source}",
                    payload.TryGetProperty("button", out var natBtn) ? natBtn.GetString() : null,
                    payload.TryGetProperty("msSinceLaunchShowStart", out var natMs) ? natMs.GetInt64() : 0,
                    payload.TryGetProperty("source", out var natSrc) ? natSrc.GetString() : null);
                break;
            case "natural_start_handler":
                _logger.LogInformation(
                    "Unity bootstrap: START handler fired {Handler} self={Self}",
                    payload.TryGetProperty("handler", out var natHandler) ? natHandler.GetString() : null,
                    payload.TryGetProperty("self", out var natSelf) ? natSelf.GetString() : null);
                break;
            case "start_button_bound":
                _logger.LogInformation(
                    "Unity bootstrap: START button bound interactable=true button={Button}",
                    payload.TryGetProperty("button", out var boundBtn) ? boundBtn.GetString() : "unknown");
                break;
            case "ship_button_bound":
                _logger.LogInformation(
                    "Unity bootstrap: ship button bound button={Button}",
                    payload.TryGetProperty("button", out var shipBoundBtn) ? shipBoundBtn.GetString() : "unknown");
                break;
            case "natural_ship_button_press":
                _logger.LogInformation(
                    "Unity bootstrap: game pressed ship button={Button} after {Ms}ms source={Source}",
                    payload.TryGetProperty("button", out var natShipBtn) ? natShipBtn.GetString() : null,
                    payload.TryGetProperty("msSinceLaunchShowStart", out var natShipMs) ? natShipMs.GetInt64() : 0,
                    payload.TryGetProperty("source", out var natShipSrc) ? natShipSrc.GetString() : null);
                break;
            case "main_menu_update_button":
                _logger.LogDebug("Unity bootstrap: LaunchShow.UpdateButton (diagnostic only)");
                break;
            case "main_menu_progress":
                _logger.LogDebug(
                    "Unity bootstrap: main menu progress phase={Phase}",
                    payload.TryGetProperty("phase", out var menuPhase) ? menuPhase.GetString() : "unknown");
                break;
            case "auto_enter_map":
                _logger.LogInformation(
                    "Unity bootstrap: auto enter map mode={Mode} gate={Gate} elapsedMs={ElapsedMs}",
                    payload.TryGetProperty("mode", out var enterMode) ? enterMode.GetString() : "unknown",
                    payload.TryGetProperty("gate", out var enterGate) ? enterGate.GetString() : "unknown",
                    payload.TryGetProperty("elapsedMs", out var elapsed) ? elapsed.GetInt64() : 0);
                break;
            case "auto_enter_map_skip":
            {
                var enterSkipReason = payload.TryGetProperty("reason", out var enterSkipProp)
                    ? enterSkipProp.GetString()
                    : "unknown";
                if (enterSkipReason is "btn_start_not_ready"
                    or "btn_start_not_bound"
                    or "ship_button_not_bound"
                    or "ship_button_same_as_start"
                    or "method_info_not_ready"
                    or "launch_show_not_ready"
                    or "launch_show_not_started")
                {
                    _logger.LogWarning(
                        "Unity bootstrap: auto enter map skipped ({Reason}) elapsedMs={ElapsedMs}",
                        enterSkipReason,
                        payload.TryGetProperty("elapsedMs", out var skipElapsedWarn) ? skipElapsedWarn.GetInt64() : 0);
                }
                else
                {
                    _logger.LogDebug(
                        "Unity bootstrap: auto enter map skipped ({Reason}) elapsedMs={ElapsedMs}",
                        enterSkipReason,
                        payload.TryGetProperty("elapsedMs", out var skipElapsedDbg) ? skipElapsedDbg.GetInt64() : 0);
                }
                break;
            }
            case "auto_enter_map_error":
                _logger.LogWarning(
                    "Unity bootstrap: auto enter map error mode={Mode}: {Message}",
                    payload.TryGetProperty("mode", out var enterErrMode) ? enterErrMode.GetString() : "unknown",
                    payload.TryGetProperty("message", out var enterErr) ? enterErr.GetString() : "unknown");
                break;
            case "map_start":
                _logger.LogInformation(
                    "Unity bootstrap: map loading started mode={Mode}",
                    payload.TryGetProperty("mode", out var mapMode) ? mapMode.GetString() : "unknown");
                break;
            case "fallback_update_web_data_attempt":
                _logger.LogWarning(
                    "Unity bootstrap: fallback UpdateWebData attempt length={Length}",
                    payload.TryGetProperty("length", out var fallbackLen) ? fallbackLen.GetInt32() : 0);
                break;
            case "fallback_update_web_data_skip":
                _logger.LogWarning(
                    "Unity bootstrap: fallback UpdateWebData skipped ({Reason}), length={Length}, preview={Preview}",
                    payload.TryGetProperty("reason", out var fallbackReason) ? fallbackReason.GetString() : "unknown",
                    payload.TryGetProperty("length", out var skippedLen) ? skippedLen.GetInt32() : 0,
                    payload.TryGetProperty("preview", out var preview) ? preview.GetString() : null);
                break;
            case "warn":
                _logger.LogWarning(
                    "Unity bootstrap warning: {Message}",
                    payload.TryGetProperty("message", out var warn) ? warn.GetString() : "unknown");
                break;
            case "enter_map":
                LogEnterMapAgentEvent(payload);
                break;
        }
    }

    private void LogEnterMapAgentEvent(JsonElement payload)
    {
        var phase = payload.TryGetProperty("phase", out var phaseProp)
            ? phaseProp.GetString()
            : "unknown";

        switch (phase)
        {
            case "loaded":
                _logger.LogInformation(
                    "Unity enter-map: agent loaded version={Version}",
                    payload.TryGetProperty("version", out var version) ? version.GetString() : "unknown");
                break;
            case "thread_attached":
                _logger.LogInformation(
                    "Unity enter-map: IL2CPP thread attached thread={Thread}",
                    payload.TryGetProperty("thread", out var thread) ? thread.GetString() : null);
                break;
            case "ready":
                _logger.LogInformation("Unity enter-map: ready");
                break;
            case "state":
                _logger.LogInformation(
                    "Unity enter-map: state start={Start} ship={Ship} attempt={Attempt}",
                    payload.TryGetProperty("btnStart", out var start) ? start.GetString() : null,
                    payload.TryGetProperty("btnShip", out var ship) ? ship.GetString() : null,
                    payload.TryGetProperty("attempt", out var attempt) ? attempt.GetInt32() : 0);
                break;
            case "invoke":
                _logger.LogInformation(
                    "Unity enter-map: invoked mode={Mode}",
                    payload.TryGetProperty("mode", out var mode) ? mode.GetString() : "unknown");
                break;
            case "invoke_error":
                _logger.LogWarning(
                    "Unity enter-map: invoke error mode={Mode}: {Reason}",
                    payload.TryGetProperty("mode", out var errorMode) ? errorMode.GetString() : "unknown",
                    payload.TryGetProperty("reason", out var errorReason) ? errorReason.GetString() : "unknown");
                break;
            case "map_start":
                _logger.LogInformation(
                    "Unity enter-map: map loading started glm={GameLoadingManager}",
                    payload.TryGetProperty("glm", out var glm) ? glm.GetString() : null);
                break;
            case "enter_game":
                _logger.LogInformation("Unity enter-map: EnterGame reached");
                break;
            case "thread_attach_error":
                _logger.LogWarning(
                    "Unity enter-map: IL2CPP thread attach failed reason={Reason}",
                    payload.TryGetProperty("reason", out var reason) ? reason.GetString() : "unknown");
                break;
        }
    }

    private void OnSessionDetached(object? sender, SessionDetachedEventArgs e)
    {
        _logger.LogWarning("Unity Frida session detached: {Reason}", e.Reason);
        lock (_gate)
        {
            _attachedPid = 0;
            DetachCore();
        }
    }

    private void DetachCore()
    {
        if (_script is not null)
            _script.Message -= OnAgentMessage;

        _rpc?.Dispose();
        _rpc = null;

        try
        {
            _script?.Unload();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unity Frida script unload failed");
        }

        _script?.Dispose();
        _script = null;

        if (_session is not null)
            _session.Detached -= OnSessionDetached;

        try
        {
            _session?.Detach();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unity Frida session detach failed");
        }

        _session?.Dispose();
        _session = null;

        _device?.Dispose();
        _device = null;

        _deviceManager?.Dispose();
        _deviceManager = null;

        _attachedPid = 0;
    }

    public void Dispose() => Detach();
}
