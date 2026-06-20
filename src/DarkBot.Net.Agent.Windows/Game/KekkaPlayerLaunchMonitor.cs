using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Detects early KekkaPlayer launch failures and surfaces a readable reason.</summary>
public sealed class KekkaPlayerLaunchMonitor : IHostedService, IDisposable
{
    private static readonly TimeSpan InitialGracePeriod = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(20);

    private readonly KekkaPlayerGameApi _kekkaApi;
    private readonly KekkaPlayerProcessLauncher _processLauncher;
    private readonly NativeGameBridge _bridge;
    private readonly ILogger<KekkaPlayerLaunchMonitor> _logger;
    private CancellationTokenSource? _monitorCts;

    public KekkaPlayerLaunchMonitor(
        KekkaPlayerGameApi kekkaApi,
        KekkaPlayerProcessLauncher processLauncher,
        NativeGameBridge bridge,
        ILogger<KekkaPlayerLaunchMonitor> logger)
    {
        _kekkaApi = kekkaApi;
        _processLauncher = processLauncher;
        _bridge = bridge;
        _logger = logger;
        _kekkaApi.PhaseChanged += OnPhaseChanged;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var previousCrash = JvmCrashDiagnostics.FindRecentCrashReport(
            AppContext.BaseDirectory,
            TimeSpan.FromHours(24));
        if (previousCrash is not null)
            _logger.LogWarning("Previous KekkaPlayer JVM crash detected: {Details}", previousCrash);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _monitorCts?.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _kekkaApi.PhaseChanged -= OnPhaseChanged;
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
    }

    private void OnPhaseChanged(GameConnectionPhase phase)
    {
        if (phase != GameConnectionPhase.WaitingForGameLoad)
            return;

        _monitorCts?.Cancel();
        _monitorCts = new CancellationTokenSource();
        _ = MonitorLaunchAsync(_monitorCts.Token);
    }

    private async Task MonitorLaunchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(InitialGracePeriod, cancellationToken).ConfigureAwait(false);

            if (_kekkaApi.Phase != GameConnectionPhase.WaitingForGameLoad)
                return;

            var failure = DiagnoseLaunchFailure();
            if (failure is not null)
            {
                ReportFailure(failure);
                return;
            }

            await Task.Delay(LoadTimeout - InitialGracePeriod, cancellationToken).ConfigureAwait(false);

            if (_kekkaApi.Phase != GameConnectionPhase.WaitingForGameLoad)
                return;

            if (_kekkaApi.IsValid)
                return;

            failure = DiagnoseStalledLoad();
            if (failure is not null)
                ReportFailure(failure);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "KekkaPlayer launch monitor task failed unexpectedly");
        }
    }

    private string? DiagnoseLaunchFailure()
    {
        var jvmCrash = JvmCrashDiagnostics.FindRecentCrashReport(
            AppContext.BaseDirectory,
            TimeSpan.FromMinutes(2));
        if (jvmCrash is not null)
            return jvmCrash;

        if (!_processLauncher.IsRunning && _processLauncher.LastProcessId is not null)
            return "KekkaPlayer Java process exited immediately after launch. "
                + "If Discord OAuth was cancelled, complete it in the browser and log in again. "
                + "See kekka-launcher-out.log and hs_err_*.log.";

        var launcherOut = Path.Combine(AppContext.BaseDirectory, "kekka-launcher-out.log");
        if (File.Exists(launcherOut))
        {
            try
            {
                string[] tail;
                using (var fs = new FileStream(launcherOut, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                    tail = sr.ReadToEnd()
                        .Split('\n')
                        .Select(l => l.TrimEnd('\r'))
                        .TakeLast(8)
                        .ToArray();

                foreach (var line in tail)
                {
                    if (line.Contains("createWindow threw", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("Exception", StringComparison.OrdinalIgnoreCase))
                        return line;
                }
            }
            catch (IOException ex)
            {
                _logger.LogDebug(ex, "Could not read kekka-launcher-out.log (still locked by pump task)");
            }
        }

        var logLines = ReadKekkaLogDiagnostics();
        if (logLines.Count > 0)
            return string.Join(" | ", logLines);

        var status = _bridge.Kekka.GetWindowStatus();
        if (status.State == KekkaPlayerWindowLoopState.Failed)
            return BuildFailureMessage(status);

        if (status.State == KekkaPlayerWindowLoopState.Exited && status.DurationMs < 5000)
            return BuildFailureMessage(status);

        var nativeError = _bridge.LastNativeError;
        if (!string.IsNullOrWhiteSpace(nativeError))
            return nativeError;

        if (status.State == KekkaPlayerWindowLoopState.Running &&
            _kekkaApi.LastInternetReadTime() == 0)
        {
            return
                "Flash window thread is running but has not downloaded any game resources yet. " +
                "If the window is blank or closes soon, verify DarkFlash.ocx and session/preloader data.";
        }

        return null;
    }

    private string? DiagnoseStalledLoad()
    {
        if (_kekkaApi.LastInternetReadTime() > 0)
        {
            return
                "Flash client downloaded resources but the game did not reach the ready state within " +
                $"{LoadTimeout.TotalSeconds:0} seconds. The client may still be loading or stuck on the login screen.";
        }

        return
            "Flash client did not finish loading within " +
            $"{LoadTimeout.TotalSeconds:0} seconds and never reported network activity. " +
            "Check KekkaPlayer.log and verify preloader/session parameters.";
    }

    private string BuildFailureMessage(KekkaPlayerWindowStatus status)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(status.Detail))
            parts.Add(status.Detail);

        var logLines = ReadKekkaLogDiagnostics();
        foreach (var line in logLines)
        {
            if (!parts.Contains(line, StringComparer.Ordinal))
                parts.Add(line);
        }

        var latestLog = KekkaPlayerLogReader.FindLatestLogPath(ResolveLogsDirectory());
        if (latestLog is not null && parts.Count == 0)
        {
            parts.Add(
                $"KekkaPlayer exited early ({status.DurationMs} ms). See log: {latestLog}");
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : $"KekkaPlayer window loop failed after {status.DurationMs} ms without additional details.";
    }

    private IReadOnlyList<string> ReadKekkaLogDiagnostics() =>
        KekkaPlayerLogReader.ReadRecentDiagnostics(ResolveLogsDirectory(), TimeSpan.FromMinutes(5));

    private string ResolveLogsDirectory() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "logs"));

    private void ReportFailure(string reason) => _kekkaApi.FailLaunch(reason);
}
