using System.Text.Json;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>
/// Unity IL2CPP client: FridaCLR attach + unity_bridge_agent.js RPC.
/// Игра должна быть уже запущена пользователем.
/// </summary>
public sealed class UnityFridaGameApi :
    IGameConnection,
    IGameInstallerProbe,
    IGameBridgeStatusSource,
    IDisposable
{
    private readonly UnityFridaSession _session;
    private readonly IServiceProvider _serviceProvider;
    private readonly GameApiOptions _options;
    private readonly ILogger<UnityFridaGameApi> _logger;
    private readonly object _statusLock = new();

    private GameConnectionPhase _phase = GameConnectionPhase.NotStarted;
    private long _pid;
    private FridaBridgeStatus? _cachedStatus;
    private DateTime _lastBridgeActivityUtc = DateTime.MinValue;
    private string? _lastFailureReason;
    private int _refreshInProgress;

    public UnityFridaGameApi(
        UnityFridaSession session,
        IOptions<GameApiOptions> options,
        ILogger<UnityFridaGameApi> logger,
        IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
        _session.AgentEvent += OnAgentEvent;
    }

    public GameApiMode Mode => GameApiMode.UnityClient;

    public GameConnectionPhase Phase => _phase;

    public bool IsLaunched => _pid != 0;

    public bool IsBridgeLive
    {
        get
        {
            if (_pid == 0 || !_session.IsAttached)
                return false;

            return (DateTime.UtcNow - _lastBridgeActivityUtc).TotalSeconds < _options.FridaBridgeStaleSec;
        }
    }

    public bool IsValid
    {
        get
        {
            var status = CurrentStatus;
            return IsBridgeLive && status?.Ready == true && _pid != 0;
        }
    }

    public string? LastFailureReason => _lastFailureReason;

    public FridaBridgeStatus? CurrentStatus
    {
        get
        {
            lock (_statusLock)
                return _cachedStatus;
        }
    }

    public event Action<GameConnectionPhase>? PhaseChanged;

    public event Action? StatusChanged;

    public event Action? BridgeDisconnected;

    public async Task AttachProcessAsync(int pid, CancellationToken cancellationToken = default)
    {
        _pid = pid;
        _lastFailureReason = null;
        SetPhase(GameConnectionPhase.WaitingForGameLoad);

        await _session.AttachAndLoadAgentAsync(pid, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Unity Frida attached to pid={Pid}", pid);

        if (RefreshStatus())
            SetPhase(GameConnectionPhase.Connected);
    }

    public void AttachProcess(long pid) =>
        AttachProcessAsync((int)pid).GetAwaiter().GetResult();

    public async Task<bool> WaitForReadyAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentStatus?.Ready == true && IsBridgeLive)
            return true;

        var deadline = DateTime.UtcNow.AddSeconds(_options.FridaReadyTimeoutSec);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (RefreshStatus() && CurrentStatus?.Ready == true && IsBridgeLive)
                return true;

            await Task.Delay(_options.FridaReadyPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return CurrentStatus?.Ready == true && IsBridgeLive;
    }

    public bool RefreshStatus()
    {
        try
        {
            var statusJson = _session.GetStatusJsonAsync().GetAwaiter().GetResult();
            var agentStatus = UnityBridgeStatusMapper.ParseStatusJson(statusJson);
            if (agentStatus is null)
                return false;

            ApplyStatus(UnityBridgeStatusMapper.ToFridaStatus(agentStatus), isSnapshot: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unity getStatus RPC failed");
            return false;
        }
    }

    public void ApplyStatus(FridaBridgeStatus status, bool isSnapshot)
    {
        _ = isSnapshot;
        RecordBridgeActivity();

        lock (_statusLock)
            _cachedStatus = status;

        if (status.Ready && _phase == GameConnectionPhase.WaitingForGameLoad)
            SetPhase(GameConnectionPhase.Connected);

        StatusChanged?.Invoke();
    }

    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0)
    {
        _ = screenManager;

        if (collectableAddress != 0)
        {
            _logger.LogWarning("Unity collect move not implemented (collectable=0x{Collectable:X})", collectableAddress);
            return;
        }

        _logger.LogInformation("Unity MoveShip target=({X},{Y})", x, y);

        if (!IsBridgeLive)
        {
            _logger.LogWarning("Unity move blocked — bridge offline");
            return;
        }

        try
        {
            var resultJson = _session.MoveToAsync((int)x, (int)y).GetAwaiter().GetResult();
            _logger.LogInformation("Unity moveTo RPC response: {Response}", resultJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unity moveTo RPC failed");
        }
    }

    public void SelectEntity(ReadOnlySpan<int> taggedArgs) =>
        _logger.LogDebug("Unity SelectEntity not implemented (args={Count})", taggedArgs.Length);

    public void UseItem(long screenManager, string itemId, int methodIndex, params long[] args) =>
        _logger.LogDebug("Unity UseItem not implemented ({ItemId})", itemId);

    public void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1) =>
        _logger.LogDebug("Unity Refine not implemented (ore={OreId})", oreId);

    public bool InvokeMethod(long objectPtr, int methodIndex, params long[] args)
    {
        _logger.LogDebug("Unity InvokeMethod not implemented (ptr=0x{Ptr:X})", objectPtr);
        return false;
    }

    public void Reload() =>
        _logger.LogDebug("Unity Reload is a no-op — game is standalone");

    public void HandleRefresh(bool useFakeDailyLogin = true)
    {
        _ = useFakeDailyLogin;

        if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
        {
            _logger.LogInformation("Unity session refresh is already in progress");
            return;
        }

        _ = RefreshSessionInBackgroundAsync();
    }

    public long LastInternetReadTime()
    {
        RefreshStatus();
        return CurrentStatus?.LastPacketActivityMs ?? Environment.TickCount64;
    }

    public void ClearCache(string pattern) => _ = pattern;

    public void MarkLaunching() => SetPhase(GameConnectionPhase.Launching);

    public void MarkWaitingForGameLoad() => SetPhase(GameConnectionPhase.WaitingForGameLoad);

    public void MarkFailed(string reason)
    {
        _lastFailureReason = reason;
        SetPhase(GameConnectionPhase.Failed);
    }

    public void ResetConnectionState()
    {
        _pid = 0;
        _lastFailureReason = null;
        _lastBridgeActivityUtc = DateTime.MinValue;

        lock (_statusLock)
            _cachedStatus = null;

        _session.Detach();
        SetPhase(GameConnectionPhase.NotStarted);
        StatusChanged?.Invoke();
    }

    public void RecordBridgeActivity() =>
        _lastBridgeActivityUtc = DateTime.UtcNow;

    void IGameInstallerProbe.RefreshStatus() => RefreshStatus();

    bool IGameInstallerProbe.TryGetInstallerAddresses(
        out long mainApplicationAddress,
        out long mainAddress,
        out long screenManagerAddress,
        out long connectionManagerAddress)
    {
        var status = CurrentStatus;
        if (status?.Ready != true)
        {
            mainApplicationAddress = 0;
            mainAddress = 0;
            screenManagerAddress = 0;
            connectionManagerAddress = 0;
            return false;
        }

        mainApplicationAddress = 1;
        mainAddress = 1;
        screenManagerAddress = 1;
        connectionManagerAddress = 1;
        return true;
    }

    public void Dispose()
    {
        _session.AgentEvent -= OnAgentEvent;
        _session.Dispose();
    }

    private void OnAgentEvent(JsonElement payload)
    {
        RecordBridgeActivity();
        FridaBridgeStatus? updated;
        lock (_statusLock)
            UnityBridgeStatusMapper.ApplyAgentEvent(_cachedStatus, payload, out updated);

        if (updated is null)
            return;

        lock (_statusLock)
            _cachedStatus = updated;

        if (updated.Ready && _phase == GameConnectionPhase.WaitingForGameLoad)
            SetPhase(GameConnectionPhase.Connected);

        StatusChanged?.Invoke();
    }

    private async Task RefreshSessionInBackgroundAsync()
    {
        try
        {
            var refresher = _serviceProvider.GetService<UnitySessionRefresher>();
            if (refresher is null)
            {
                _logger.LogWarning("Unity session refresh is unavailable — refresher service is not configured");
                return;
            }

            var refreshed = await refresher.TryRefreshSessionAsync().ConfigureAwait(false);

            if (!refreshed)
                _logger.LogWarning("Unity session refresh did not complete");
        }
        finally
        {
            Interlocked.Exchange(ref _refreshInProgress, 0);
        }
    }

    private void SetPhase(GameConnectionPhase phase)
    {
        if (_phase == phase)
            return;

        _phase = phase;
        PhaseChanged?.Invoke(phase);

        if (phase == GameConnectionPhase.Failed)
            BridgeDisconnected?.Invoke();
    }
}
