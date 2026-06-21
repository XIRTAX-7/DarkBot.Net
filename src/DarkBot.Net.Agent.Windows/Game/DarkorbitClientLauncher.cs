using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Spawns Darkorbit-client (Electron) with dosid — replaces KekkaPlayer window.</summary>
public sealed class DarkorbitClientLauncher
{
    private readonly GameApiOptions _options;
    private readonly ILogger<DarkorbitClientLauncher> _logger;
    private Process? _process;

    public DarkorbitClientLauncher(IOptions<GameApiOptions> options, ILogger<DarkorbitClientLauncher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int? LastProcessId => _process is { HasExited: false } ? _process.Id : null;

    public bool IsRunning => _process is { HasExited: false };

    public void Launch(GameLaunchParameters launch)
    {
        if (IsRunning)
        {
            _logger.LogInformation("Darkorbit-client already running (pid {Pid})", _process!.Id);
            return;
        }

        var clientRoot = DarkorbitClientPaths.Resolve(_options.DarkorbitClientPath);
        EnsureBotSettings(clientRoot);
        EnsureNpmDependencies(clientRoot);

        var instanceUri = new Uri(launch.InstanceUrl);
        var baseUrl = instanceUri.GetLeftPart(UriPartial.Authority);
        // Full URL dosid — client opens game window with internalMapRevolution (START button).
        var dosidUrl = $"{baseUrl}/?dosid={launch.Sid}";
        var electron = DarkorbitClientPaths.ResolveElectronExecutable(clientRoot);

        var argumentSummary = $"--dosid {dosidUrl}";

        _logger.LogInformation(
            "Starting Darkorbit-client: {Electron} {Args} (cwd={Root})",
            electron,
            argumentSummary,
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

        _logger.LogInformation("Darkorbit-client electron pid {Pid}, log: {Log}", _process.Id, logPath);
    }

    public void Stop()
    {
        if (_process is null)
            return;

        try
        {
            if (!_process.HasExited)
            {
                _logger.LogInformation("Stopping Darkorbit-client (pid {Pid})", _process.Id);
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop Darkorbit-client");
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    // Output pump removed — Electron GUI must not have redirected streams.

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
            settings["Packet"] = _options.EnablePacketBridge;
            settings["PacketTimeout"] = 5000;
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
