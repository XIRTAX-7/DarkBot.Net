using System.Diagnostics;
using System.Text;
using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>
/// Starts KekkaPlayer in a standalone Java process — same sequence as Java DarkBot
/// (<see cref="eu.darkbot.bridge.KekkaMinimalLauncher"/> — full Main init, MainGui hidden after startup).
/// Requires the signed release <c>DarkBot.jar</c> (not a Gradle fat jar) for KekkaPlayer JNI.
/// Avoids embedding JVM in the Avalonia process (fatal JVM errors would kill the UI).
/// </summary>
public sealed class KekkaPlayerProcessLauncher
{
    private readonly ILogger<KekkaPlayerProcessLauncher> _logger;
    private Process? _process;
    private int? _lastProcessId;
    private int? _lastExitCode;

    public KekkaPlayerProcessLauncher(ILogger<KekkaPlayerProcessLauncher> logger) => _logger = logger;

    public bool IsRunning => _process is { HasExited: false };

    public int? LastProcessId => IsRunning ? _process!.Id : _lastProcessId;

    public int? LastExitCode => IsRunning ? null : _lastExitCode;

    public void Launch(
        GameLaunchParameters launch,
        string flashOcxPath,
        GameApiOptions options,
        int proxyPort)
    {
        if (IsRunning)
        {
            _logger.LogWarning("KekkaPlayer Java process already running (pid {Pid})", _process!.Id);
            return;
        }

        var workingDir = AppContext.BaseDirectory;
        var libDir = NativeBridgePaths.ResolveLibDir(options.LibPath);
        var classesDir = NativeBridgePaths.ResolveClassesDir(options.ClassesPath);

        if (!NativeBridgePaths.EnsureDarkBotJarInLib(libDir, options.DarkBotJarPath, _logger))
        {
            throw new InvalidOperationException(
                "DarkBot.jar is required for KekkaPlayer. Copy it to ./lib/DarkBot.jar or set DarkBot:DarkBotJarPath.");
        }

        KekkaLaunchDebugWriter.TryWrite(launch, flashOcxPath, options, proxyPort);

        var propsPath = Path.Combine(workingDir, "launch.properties");
        var argFile = Path.Combine(workingDir, "kekka-launcher.jvmargs");
        var classPath = NativeBridgePaths.BuildBridgeClassPath(classesDir, libDir, options.DarkBotJarPath);
        WriteJvmArgFile(argFile, workingDir, libDir, classPath, propsPath);

        var java = ResolveJavaExecutable();
        var logOut = Path.Combine(workingDir, "kekka-launcher-out.log");
        var logErr = Path.Combine(workingDir, "kekka-launcher-err.log");

        _logger.LogInformation(
            "Starting KekkaPlayer Java process (like Java DarkBot): java=@{ArgFile}, user.dir={WorkingDir}, lib={LibDir}",
            argFile,
            workingDir,
            libDir);

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = java,
            Arguments = $"@{argFile}",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });

        if (_process is null)
            throw new InvalidOperationException("Failed to start KekkaPlayer Java process.");

        _logger.LogInformation("KekkaPlayer Java process started (pid {Pid})", _process.Id);
        _lastProcessId = _process.Id;
        _lastExitCode = null;

        _ = Task.Run(() => PumpOutputAsync(_process, logOut, logErr));
        _ = Task.Run(() => MonitorExitAsync(_process, workingDir));
    }

    private async Task PumpOutputAsync(Process process, string logOut, string logErr)
    {
        try
        {
            var outTask = PumpStreamAsync(process.StandardOutput, logOut);
            var errTask = PumpStreamAsync(process.StandardError, logErr);
            await Task.WhenAll(outTask, errTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "KekkaPlayer Java output pump failed");
        }
    }

    private static async Task PumpStreamAsync(StreamReader reader, string path)
    {
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var file = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            await file.WriteLineAsync(line).ConfigureAwait(false);
    }

    private async Task MonitorExitAsync(Process process, string workingDir)
    {
        try
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
            _lastExitCode = process.ExitCode;
            _logger.LogWarning("KekkaPlayer Java process exited with code {Code}", process.ExitCode);

            var crash = JvmCrashDiagnostics.FindRecentCrashReport(workingDir, TimeSpan.FromMinutes(1));
            if (crash is not null)
                _logger.LogError("KekkaPlayer JVM crash: {Details}", crash);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "KekkaPlayer process monitor failed");
        }
    }

    private static void WriteJvmArgFile(
        string argFile,
        string workingDir,
        string libDir,
        string classPath,
        string propertiesFile)
    {
        var lines = new[]
        {
            $"-Duser.dir={ToJavaPath(workingDir)}",
            $"-Djava.library.path={ToJavaPath(libDir)}",
            $"-Ddarkbot.kekka.library={ToJavaPath(Path.Combine(libDir, "KekkaPlayer.dll"))}",
            "-cp",
            ToJavaPath(classPath),
            "eu.darkbot.bridge.KekkaMinimalLauncher",
            $"@{ToJavaPath(propertiesFile)}",
        };

        File.WriteAllLines(argFile, lines, Encoding.ASCII);
    }

    private static string ToJavaPath(string path) => Path.GetFullPath(path).Replace('\\', '/');

    private static string ResolveJavaExecutable()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var candidate = Path.Combine(javaHome, "bin", "java.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        return "java";
    }
}
