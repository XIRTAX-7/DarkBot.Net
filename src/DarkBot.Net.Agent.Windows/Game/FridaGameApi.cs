using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>
/// Pepper / Darkorbit-client path: game state via Frida AVM (/status), actions via darkDev HTTP (:44570).
/// Raw ReadInt/Long/Double remain for BotInstaller pattern search only — managers use <see cref="IGameFridaProbe"/>.
/// </summary>
public sealed class FridaGameApi : IGameConnection, IDisposable
{
    private static readonly byte[] MainApplicationPattern =
    [
        1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0,
        0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0
    ];

    private readonly NativeGameBridge _bridge;
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
    private long _lastStatusRefreshMs;

    public FridaGameApi(
        NativeGameBridge bridge,
        ElectronControlClient control,
        GamePacketReader packetReader,
        IOptions<GameApiOptions> options,
        ILogger<FridaGameApi> logger)
    {
        _bridge = bridge;
        _control = control;
        _packetReader = packetReader;
        _options = options.Value;
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    public GameApiMode Mode => GameApiMode.FridaClient;

    public GameConnectionPhase Phase => _phase;

    public bool IsLaunched => _pid != 0;

    public bool IsValid
    {
        get
        {
            var status = CurrentStatus;
            return status?.Ready == true && _pid != 0;
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

    public void AttachProcess(long pid)
    {
        _bridge.EnsureInitialized();
        _bridge.OpenProcess(pid);
        _pid = pid;
        LastFailureReason = null;
        SetPhase(GameConnectionPhase.WaitingForGameLoad);
        _logger.LogInformation("FridaClient attached to Pepper pid={Pid}, bridge http://127.0.0.1:{Port}", pid, _options.FridaApiPort);
        RefreshStatus();
        if (IsValid)
            SetPhase(GameConnectionPhase.Connected);
    }

    public bool RefreshStatus()
    {
        var now = Environment.TickCount64;
        if (now - _lastStatusRefreshMs < 250)
            return _cachedStatus is not null;

        _lastStatusRefreshMs = now;

        try
        {
            var status = _http.GetFromJsonAsync<FridaBridgeStatus>(StatusUrl()).GetAwaiter().GetResult();
            lock (_statusLock)
                _cachedStatus = status;

            if (status?.LastPacketActivityMs > 0)
                _lastActivityMs = status.LastPacketActivityMs;

            if (status?.Ready == true && _phase == GameConnectionPhase.WaitingForGameLoad)
                SetPhase(GameConnectionPhase.Connected);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Frida /status poll failed");
            return false;
        }
    }

    public int ReadInt(long address)
    {
        EnsureAttached();
        return _bridge.ReadInt(address);
    }

    public long ReadLong(long address)
    {
        EnsureAttached();
        return _bridge.ReadLong(address);
    }

    public double ReadDouble(long address)
    {
        EnsureAttached();
        return _bridge.ReadDouble(address);
    }

    public long SearchPattern(ReadOnlySpan<byte> pattern)
    {
        RefreshStatus();
        var status = CurrentStatus;
        if (status?.Ready != true)
            return 0;

        if (pattern.SequenceEqual(MainApplicationPattern))
            return FridaBridgeStatus.ParsePtr(status.MainApplicationAddress) + 228;

        return 0;
    }

    public long SearchClassClosure(Func<long, bool> pattern) => 0;

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

    public void SelectEntity(ReadOnlySpan<int> taggedArgs)
    {
        PostAction("/select", new { args = taggedArgs.ToArray() });
    }

    public void UseItem(long screenManager, string itemId, int methodIndex, params long[] args)
    {
        PostAction("/useItem", new
        {
            itemId,
            methodIndex,
            args = args.Select(a => $"0x{a:x}").ToArray()
        });
    }

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

    public IReadOnlyList<GameProcessInfo> GetProcesses() => _bridge.GetProcesses();

    public void OpenProcess(long pid) => AttachProcess(pid);

    public void Dispose() => _http.Dispose();

    private string BaseUrl() => $"http://127.0.0.1:{_options.FridaApiPort}";

    private string StatusUrl() => $"{BaseUrl()}/status";

    private FridaActionResult? PostAction(string path, object body)
    {
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

    private void EnsureAttached()
    {
        if (_pid == 0)
            throw new InvalidOperationException("No Pepper process attached.");
    }

    public void MarkLaunching() => SetPhase(GameConnectionPhase.Launching);

    public void MarkWaitingForGameLoad() => SetPhase(GameConnectionPhase.WaitingForGameLoad);

    public void MarkFailed(string reason)
    {
        LastFailureReason = reason;
        SetPhase(GameConnectionPhase.Failed);
    }

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
        public string? Error { get; init; }
    }
}
