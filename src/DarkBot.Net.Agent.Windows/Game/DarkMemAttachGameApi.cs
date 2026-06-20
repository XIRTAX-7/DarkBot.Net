using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Game;

public sealed class DarkMemAttachGameApi : IGameConnection
{
    private readonly NativeGameBridge _bridge;
    private readonly ILogger<DarkMemAttachGameApi> _logger;
    private GameConnectionPhase _phase = GameConnectionPhase.NotStarted;
    private long _pid;

    public DarkMemAttachGameApi(NativeGameBridge bridge, ILogger<DarkMemAttachGameApi> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    public GameApiMode Mode => GameApiMode.DarkMemAttach;

    public GameConnectionPhase Phase => _phase;

    public bool IsLaunched => _pid != 0;

    public bool IsValid => _pid != 0;

    public string? LastFailureReason => null;

    public event Action<GameConnectionPhase>? PhaseChanged;

    public void AttachProcess(long pid)
    {
        _bridge.EnsureInitialized();
        _bridge.OpenProcess(pid);
        _pid = pid;
        SetPhase(GameConnectionPhase.Connected);
        _logger.LogInformation("Attached to external Flash process pid={Pid}", pid);
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

    public long SearchPattern(ReadOnlySpan<byte> pattern) => 0;

    public long SearchClassClosure(Func<long, bool> pattern) => 0;

    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0)
    {
        if (_bridge.IsKekkaAvailable)
            _bridge.Kekka.MoveShip(screenManager, x, y, collectableAddress);
    }

    public void Reload() { }

    public void HandleRefresh(bool useFakeDailyLogin = true) { }

    public long LastInternetReadTime() => 0;

    public void ClearCache(string pattern) { }

    public IReadOnlyList<GameProcessInfo> GetProcesses() => _bridge.GetProcesses();

    public void OpenProcess(long pid) => AttachProcess(pid);

    private void EnsureAttached()
    {
        if (_pid == 0)
            throw new InvalidOperationException("No process attached.");
    }

    private void SetPhase(GameConnectionPhase phase)
    {
        if (_phase == phase)
            return;

        _phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
