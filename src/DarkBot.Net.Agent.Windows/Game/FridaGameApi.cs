using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>
/// Darkorbit-client path: game state via Frida bridge WS push, actions via HTTP POST (:44570).
/// Frida-only — no DarkMem / external memory reads.
/// </summary>
public sealed class FridaGameApi : IGameConnection, IDisposable
{
    private readonly HttpClient _http;
    private readonly ElectronControlClient _control;
    private readonly GamePacketReader _packetReader;
    private readonly GameApiOptions _options;
    private readonly ILogger<FridaGameApi> _logger;
    private readonly object _statusLock = new();
    private GameConnectionPhase _phase = GameConnectionPhase.NotStarted;
    private long _pid;
    private FridaBridgeStatus? _cachedStatus;
    private long _lastActivityMs;
    private long _lastHttpFallbackMs;
    private DateTime _lastBridgeActivityUtc = DateTime.MinValue;
    private bool _bridgeWsConnected;
    private bool _receivedSnapshot;
    private TaskCompletionSource<bool>? _readyTcs;

    public FridaGameApi(
        ElectronControlClient control,
        GamePacketReader packetReader,
        IOptions<GameApiOptions> options,
        ILogger<FridaGameApi> logger)
    {
        _control = control;
        _packetReader = packetReader;
        _options = options.Value;
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    public GameApiMode Mode => GameApiMode.FridaClient;

    public GameConnectionPhase Phase => _phase;

    public bool IsLaunched => _pid != 0;

    public bool IsBridgeLive
    {
        get
        {
            if (!_bridgeWsConnected || !_receivedSnapshot)
                return false;

            var staleSec = _options.FridaBridgeStaleSec;
            return (DateTime.UtcNow - _lastBridgeActivityUtc).TotalSeconds < staleSec;
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

    public string? LastFailureReason { get; private set; }

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

    public void NotifyBridgeConnected()
    {
        _bridgeWsConnected = true;
        _receivedSnapshot = false;
        _lastBridgeActivityUtc = DateTime.UtcNow;
    }

    public void NotifyBridgeDisconnected()
    {
        _bridgeWsConnected = false;
        _receivedSnapshot = false;
        if (_phase == GameConnectionPhase.Connected)
            SetPhase(GameConnectionPhase.WaitingForGameLoad);
        StatusChanged?.Invoke();
    }

    public void RecordBridgeActivity() =>
        _lastBridgeActivityUtc = DateTime.UtcNow;

    public void ApplyStatus(FridaBridgeStatus status, bool isSnapshot)
    {
        RecordBridgeActivity();
        if (isSnapshot)
            _receivedSnapshot = true;

        lock (_statusLock)
            _cachedStatus = status;

        if (status.LastPacketActivityMs > 0)
            _lastActivityMs = status.LastPacketActivityMs;

        if (status.Ready && _phase == GameConnectionPhase.WaitingForGameLoad)
            SetPhase(GameConnectionPhase.Connected);

        StatusChanged?.Invoke();

        if (status.Ready)
            CompleteReadyWait(true);
    }

    public void AttachProcess(long pid)
    {
        _pid = pid;
        LastFailureReason = null;
        ResetReadyWait();
        SetPhase(GameConnectionPhase.WaitingForGameLoad);
        _logger.LogInformation(
            "FridaClient tracking Pepper pid={Pid}, bridge ws://127.0.0.1:{Port}/ws",
            pid,
            _options.FridaApiPort);
        RefreshStatus();
        if (IsValid)
            SetPhase(GameConnectionPhase.Connected);
    }

    public async Task<bool> WaitForReadyAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentStatus?.Ready == true && IsBridgeLive)
            return true;

        ResetReadyWait();
        var tcs = _readyTcs!;
        using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        var deadline = DateTime.UtcNow.AddSeconds(_options.FridaReadyTimeoutSec);
        while (DateTime.UtcNow < deadline)
        {
            if (CurrentStatus?.Ready == true && IsBridgeLive)
                return true;

            TryHttpFallback();

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            var waitMs = (int)Math.Min(remaining.TotalMilliseconds, _options.FridaReadyPollIntervalMs);
            try
            {
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(waitMs, cancellationToken))
                    .ConfigureAwait(false);
                if (completed == tcs.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (await tcs.Task.ConfigureAwait(false) && IsBridgeLive)
                        return true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }

        return CurrentStatus?.Ready == true && IsBridgeLive;
    }

    public bool RefreshStatus()
    {
        lock (_statusLock)
        {
            if (_cachedStatus is not null && IsBridgeLive)
                return true;
        }

        return TryHttpFallback();
    }

    private bool TryHttpFallback()
    {
        var now = Environment.TickCount64;
        if (now - _lastHttpFallbackMs < 500)
            return _cachedStatus is not null;

        _lastHttpFallbackMs = now;

        try
        {
            var status = _http.GetFromJsonAsync<FridaBridgeStatus>(StatusUrl()).GetAwaiter().GetResult();
            if (status is not null)
                ApplyStatus(status, isSnapshot: true);

            return status is not null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Frida /status HTTP fallback failed");
            return false;
        }
    }

    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0)
    {
        if (collectableAddress != 0)
        {
            PostAction("/collect", new
            {
                x,
                y,
                collectableAdr = $"0x{collectableAddress:x}"
            });
            return;
        }

        PostAction("/move", new { x, y });
    }

    public void SelectEntity(ReadOnlySpan<int> taggedArgs) =>
        PostAction("/select", new { args = taggedArgs.ToArray() });

    public void UseItem(long screenManager, string itemId, int methodIndex, params long[] args) =>
        PostAction("/useItem", new
        {
            itemId,
            methodIndex,
            args = args.Select(a => $"0x{a:x}").ToArray()
        });

    public void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1)
    {
        var body = new Dictionary<string, object>
        {
            ["refineUtilAddress"] = $"0x{refineUtilAddress:x}",
            ["oreId"] = oreId,
            ["amount"] = amount
        };
        if (methodIndex >= 0)
            body["methodIndex"] = methodIndex;

        PostAction("/refine", body);
    }

    public bool InvokeMethod(long objectPtr, int methodIndex, params long[] args)
    {
        var result = PostAction("/invoke", new
        {
            objectPtr = $"0x{objectPtr:x}",
            methodIndex,
            args = args.Select(FormatInvokeArg).ToArray()
        });
        return result?.Ok == true;
    }

    public void Reload()
    {
        try
        {
            _control.ReloadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Darkorbit-client reload failed");
        }
    }

    public void HandleRefresh(bool useFakeDailyLogin = true)
    {
        _logger.LogInformation("FridaClient refresh — reloading Darkorbit-client page");
        SetPhase(GameConnectionPhase.WaitingForGameLoad);
        lock (_statusLock)
            _cachedStatus = null;

        _receivedSnapshot = false;
        ResetReadyWait();
        Reload();
    }

    public long LastInternetReadTime()
    {
        RefreshStatus();

        var packetMs = _packetReader.LastPacketAt?.ToUnixTimeMilliseconds() ?? 0;
        var activityMs = Math.Max(_lastActivityMs, packetMs);
        return activityMs > 0 ? activityMs : Environment.TickCount64;
    }

    public void ClearCache(string pattern) { }

    public void Dispose() => _http.Dispose();

    private string BaseUrl() => $"http://127.0.0.1:{_options.FridaApiPort}";

    private string StatusUrl() => $"{BaseUrl()}/status";

    private FridaActionResult? PostAction(string path, object body)
    {
        if (!IsBridgeLive)
        {
            _logger.LogWarning("Frida POST {Path} blocked — bridge offline (no fresh WS status)", path);
            return new FridaActionResult { Ok = false, Error = "bridge offline" };
        }

        try
        {
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = _http.PostAsync($"{BaseUrl()}{path}", content).GetAwaiter().GetResult();
            var result = response.Content.ReadFromJsonAsync<FridaActionResult>().GetAwaiter().GetResult();
            if (result?.Ok != true)
                _logger.LogWarning("Frida {Path} failed: {Error}", path, result?.Error ?? response.ReasonPhrase);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Frida POST {Path} failed", path);
            return null;
        }
    }

    private static object FormatInvokeArg(long value) =>
        value > 0x10000 ? $"0x{value:x}" : value;

    public void MarkLaunching() => SetPhase(GameConnectionPhase.Launching);

    public void MarkWaitingForGameLoad() => SetPhase(GameConnectionPhase.WaitingForGameLoad);

    public void MarkFailed(string reason)
    {
        LastFailureReason = reason;
        SetPhase(GameConnectionPhase.Failed);
    }

    private void ResetReadyWait()
    {
        _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void CompleteReadyWait(bool success) =>
        _readyTcs?.TrySetResult(success);

    private void SetPhase(GameConnectionPhase phase)
    {
        if (_phase == phase)
            return;

        _phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    private sealed class FridaActionResult
    {
        public bool Ok { get; init; }
        public bool Accepted { get; init; }
        public string? CommandId { get; init; }
        public string? Error { get; init; }
    }
}
