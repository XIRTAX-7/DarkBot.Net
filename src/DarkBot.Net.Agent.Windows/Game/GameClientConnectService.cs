using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Waits for Pepper + Frida HTTP after Darkorbit-client launch; attaches DarkMem.</summary>
public sealed class GameClientConnectService
{
    private readonly NativeGameBridge _bridge;
    private readonly FridaGameApi _frida;
    private readonly ElectronControlClient _control;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientConnectService> _logger;

    public GameClientConnectService(
        NativeGameBridge bridge,
        FridaGameApi frida,
        ElectronControlClient control,
        IOptions<GameApiOptions> options,
        ILogger<GameClientConnectService> logger)
    {
        _bridge = bridge;
        _frida = frida;
        _control = control;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var existingPids = _bridge.IsInitialized
            ? _bridge.GetProcesses().Select(p => p.Pid).ToHashSet()
            : [];

        var pepperPid = await WaitForNewPepperPidAsync(existingPids, cancellationToken).ConfigureAwait(false);
        if (pepperPid == 0)
        {
            return GameClientConnectResult.Fail(
                "Pepper Flash process not found. Open the game map in Darkorbit-client.");
        }

        _frida.AttachProcess(pepperPid);
        _logger.LogInformation("Attached to Pepper pid={Pid}", pepperPid);

        var fridaReady = await WaitForFridaReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!fridaReady)
        {
            return GameClientConnectResult.Fail(
                $"Frida game API not ready on :{_options.FridaApiPort}. Stay on the map and retry.");
        }

        return GameClientConnectResult.Ok(pepperPid);
    }

    private async Task<int> WaitForNewPepperPidAsync(HashSet<int> existingPids, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.ClientConnectTimeoutSec);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var controlPid = await TryGetPepperPidFromControlAsync(cancellationToken).ConfigureAwait(false);
            if (controlPid > 0 && !existingPids.Contains(controlPid))
            {
                _logger.LogInformation("Detected Pepper pid={Pid} via control WS", controlPid);
                return controlPid;
            }

            if (_bridge.IsInitialized)
            {
                foreach (var proc in _bridge.GetProcesses())
                {
                    if (existingPids.Contains(proc.Pid))
                        continue;

                    if (IsPepperProcess(proc.Name))
                    {
                        _logger.LogInformation("Detected Pepper process {Name} pid={Pid}", proc.Name, proc.Pid);
                        return proc.Pid;
                    }
                }
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

    private async Task<bool> WaitForFridaReadyAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.FridaReadyTimeoutSec);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _frida.RefreshStatus();
            if (_frida.CurrentStatus?.Ready == true)
                return true;

            await Task.Delay(_options.FridaReadyPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private static bool IsPepperProcess(string name) =>
        name.Contains("ppapi", StringComparison.OrdinalIgnoreCase)
        || name.Contains("pepper", StringComparison.OrdinalIgnoreCase)
        || name.Contains("pepflash", StringComparison.OrdinalIgnoreCase)
        || name.Contains("flash", StringComparison.OrdinalIgnoreCase);
}

public sealed class GameClientConnectResult
{
    public bool Success { get; init; }
    public int PepperPid { get; init; }
    public string? Error { get; init; }

    public static GameClientConnectResult Ok(int pid) => new() { Success = true, PepperPid = pid };

    public static GameClientConnectResult Fail(string error) => new() { Success = false, Error = error };
}
