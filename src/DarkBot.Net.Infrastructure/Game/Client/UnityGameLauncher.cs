using System.Diagnostics;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Client;

/// <summary>Запуск Unity-клиента DarkOrbit.exe и подготовка credentials для TS bootstrap.</summary>
public sealed class UnityGameLauncher
{
    private readonly GameApiOptions _options;
    private readonly UnityProcessFinder _processFinder;
    private readonly UnitySessionBootstrapStore _bootstrapStore;
    private readonly ILogger<UnityGameLauncher> _logger;
    private readonly object _processGate = new();
    private Process? _process;

    public UnityGameLauncher(
        IOptions<GameApiOptions> options,
        UnityProcessFinder processFinder,
        UnitySessionBootstrapStore bootstrapStore,
        ILogger<UnityGameLauncher> logger)
    {
        _options = options.Value;
        _processFinder = processFinder;
        _bootstrapStore = bootstrapStore;
        _logger = logger;
    }

    public event EventHandler? ClientProcessExited;

    public bool IsRunning
    {
        get
        {
            lock (_processGate)
            {
                if (IsTrackedProcessAlive())
                    return true;
            }

            return _processFinder.FindRunningProcessId() > 0;
        }
    }

    public int? LastProcessId
    {
        get
        {
            lock (_processGate)
            {
                if (_process is { HasExited: false })
                    return _process.Id;
            }

            var pid = _processFinder.FindRunningProcessId();
            return pid > 0 ? pid : null;
        }
    }

    public void Launch(GameLaunchParameters launch)
    {
        var existingPid = _processFinder.FindRunningProcessId();
        if (existingPid > 0)
        {
            _logger.LogInformation(
                "Unity game already running (pid {Pid}) — skipping spawn",
                existingPid);
            StageBootstrapSession(launch);
            return;
        }

        lock (_processGate)
        {
            if (IsTrackedProcessAlive())
            {
                _logger.LogInformation("Unity game already running (pid {Pid})", _process!.Id);
                StageBootstrapSession(launch);
                return;
            }

            StageBootstrapSession(launch);

            var exePath = UnityGamePaths.ResolveExecutable(_options);
            var workingDirectory = UnityGamePaths.ResolveInstallDirectory(_options);

            _logger.LogInformation(
                "Starting Unity game: {Exe} (cwd={Cwd})",
                exePath,
                workingDirectory);

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
            };

            _process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start Unity game: {exePath}");

            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;

            _logger.LogInformation("Unity game started pid={Pid}", _process.Id);
        }
    }

    public async Task<int> WaitForProcessIdAsync(CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.ClientConnectTimeoutSec);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pid = LastProcessId;
            if (pid > 0)
                return pid.Value;

            await Task.Delay(_options.ConnectPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Task.Run(async () =>
        {
            Process? tracked;
            lock (_processGate)
            {
                tracked = _process;
                _process = null;
            }

            if (tracked is not null)
                await StopProcessAsync(tracked, cancellationToken).ConfigureAwait(false);

            var orphanPid = _processFinder.FindRunningProcessId();
            if (orphanPid > 0)
            {
                try
                {
                    var orphan = Process.GetProcessById(orphanPid);
                    await StopProcessAsync(orphan, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to stop orphan Unity process pid={Pid}", orphanPid);
                }
            }
        }, cancellationToken);

    private void StageBootstrapSession(GameLaunchParameters launch)
    {
        if (!_options.UnityAuthViaHook)
            return;

        if (string.IsNullOrWhiteSpace(launch.Username) || string.IsNullOrWhiteSpace(launch.Password))
        {
            _logger.LogWarning("Unity WebView autologin: credentials missing in launch parameters");
            return;
        }

        _bootstrapStore.Set(new UnityWebGlSession(launch.Username, launch.Password));

        _logger.LogInformation(
            "Unity WebView autologin scheduled for user {Username}",
            launch.Username);
    }

    private bool IsTrackedProcessAlive() =>
        _process is { HasExited: false };

    private void OnProcessExited(object? sender, EventArgs e)
    {
        lock (_processGate)
        {
            if (sender is Process process && ReferenceEquals(_process, process))
                _process = null;
        }

        ClientProcessExited?.Invoke(this, EventArgs.Empty);
    }

    private async Task StopProcessAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            if (!process.HasExited)
            {
                _logger.LogInformation("Stopping Unity game (pid {Pid})", process.Id);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Unity game stopped (pid {Pid})", process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop Unity game process (pid {Pid})", process.Id);
        }
        finally
        {
            process.Dispose();
        }
    }
}
