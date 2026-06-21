using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Spawns Darkorbit-client (Electron) with dosid — replaces KekkaPlayer window.</summary>
public sealed class DarkorbitClientLauncher
{
    private readonly GameApiOptions _options;
    private readonly ILogger<DarkorbitClientLauncher> _logger;
    private readonly object _processGate = new();
    private Process? _process;

    public event EventHandler? ClientProcessExited;

    public DarkorbitClientLauncher(IOptions<GameApiOptions> options, ILogger<DarkorbitClientLauncher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int? LastProcessId
    {
        get
        {
            lock (_processGate)
                return _process is { HasExited: false } ? _process.Id : null;
        }
    }

    public bool IsRunning
    {
        get
        {
            lock (_processGate)
                return _process is { HasExited: false };
        }
    }

    public void Launch(GameLaunchParameters launch)
    {
        lock (_processGate)
        {
            if (IsRunningUnlocked())
            {
                _logger.LogInformation("Darkorbit-client already running (pid {Pid})", _process!.Id);
                return;
            }

            var clientRoot = DarkorbitClientPaths.Resolve(_options.DarkorbitClientPath);
            EnsureBotSettings(clientRoot);
            EnsureNpmDependencies(clientRoot);

            var instanceUri = new Uri(launch.InstanceUrl);
            var baseUrl = instanceUri.GetLeftPart(UriPartial.Authority);
            var dosidUrl = $"{baseUrl}/?dosid={launch.Sid}";
            var electron = DarkorbitClientPaths.ResolveElectronExecutable(clientRoot);

            _logger.LogInformation(
                "Starting Darkorbit-client: {Electron} --dosid {DosidUrl} (cwd={Root})",
                electron,
                dosidUrl,
                clientRoot);

            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "darkorbit-client.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"--- started {DateTimeOffset.Now:u} pid=pending exe={electron}{Environment.NewLine}");

            var startInfo = new ProcessStartInfo
            {
                FileName = electron,
                WorkingDirectory = clientRoot,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(clientRoot);
            startInfo.ArgumentList.Add("--dosid");
            startInfo.ArgumentList.Add(dosidUrl);

            _process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start Darkorbit-client.");

            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;

            _logger.LogInformation("Darkorbit-client electron pid {Pid}, log: {Log}", _process.Id, logPath);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        lock (_processGate)
        {
            if (sender is Process process && ReferenceEquals(_process, process))
                _process = null;
        }

        ClientProcessExited?.Invoke(this, EventArgs.Empty);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            Process? tracked;
            lock (_processGate)
            {
                tracked = _process;
                _process = null;
            }

            if (tracked is not null)
                await StopTrackedProcessAsync(tracked, cancellationToken).ConfigureAwait(false);

            try
            {
                var clientRoot = DarkorbitClientPaths.Resolve(_options.DarkorbitClientPath);
                await KillOrphanedElectronProcessesAsync(clientRoot, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Skipping orphaned Darkorbit-client process scan");
            }
        }, cancellationToken);
    }

    public void Stop() => StopAsync().GetAwaiter().GetResult();

    private async Task StopTrackedProcessAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            if (!process.HasExited)
            {
                _logger.LogInformation("Stopping Darkorbit-client (pid {Pid})", process.Id);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Darkorbit-client stopped (pid {Pid})", process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop tracked Darkorbit-client process (pid {Pid})", process.Id);
        }
        finally
        {
            process.Dispose();
        }
    }

    private async Task KillOrphanedElectronProcessesAsync(string clientRoot, CancellationToken cancellationToken)
    {
        string expectedElectron;
        try
        {
            expectedElectron = Path.GetFullPath(DarkorbitClientPaths.ResolveElectronExecutable(clientRoot));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skipping orphaned Electron scan — executable not resolved");
            return;
        }

        foreach (var process in Process.GetProcesses())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (process.HasExited)
                    continue;

                var modulePath = TryGetMainModulePath(process);
                if (modulePath is null
                    || !string.Equals(modulePath, expectedElectron, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _logger.LogWarning(
                    "Killing orphaned Darkorbit-client Electron process (pid {Pid}, exe={Exe})",
                    process.Id,
                    modulePath);

                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Could not inspect or kill process {Pid}", process.Id);
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static string? TryGetMainModulePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private bool IsRunningUnlocked() => _process is { HasExited: false };

    private void EnsureBotSettings(string clientRoot)
    {
        try
        {
            var settingsPath = DarkorbitClientPaths.ResolveElectronSettingsPath();
            var directory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(directory);

            JsonObject root;
            if (File.Exists(settingsPath))
            {
                root = JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject() ?? new JsonObject();
            }
            else
            {
                var defaultsPath = Path.Combine(clientRoot, "defaultSettings.json");
                root = File.Exists(defaultsPath)
                    ? JsonNode.Parse(File.ReadAllText(defaultsPath))!.AsObject()
                    : new JsonObject();
            }

            root["hideMasterRegister"] = true;
            root["autoClose"] = false;

            var settings = root["Settings"] as JsonObject ?? new JsonObject();
            settings["NoSandbox"] = true;
            settings["Movement"] = true;
            settings["MovementPort"] = _options.FridaApiPort;
            settings["MovementTimeout"] = _options.MovementTimeoutMs;
            settings["Control"] = true;
            settings["ControlPort"] = _options.ControlPort;
            root["Settings"] = settings;
            root["check"] = true;

            File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            _logger.LogInformation("Client settings patched: {Path}", settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not patch Darkorbit-client settings — enable Movement + NoSandbox manually");
        }
    }

    private static void EnsureNpmDependencies(string clientRoot)
    {
        if (DarkorbitClientPaths.HasNpmDependencies(clientRoot))
            return;

        throw new InvalidOperationException(
            $"Darkorbit-client dependencies missing in {clientRoot}. Run: npm install");
    }
}
