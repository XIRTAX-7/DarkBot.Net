using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Bridge;

public static class NativeBridgePaths
{
    public static string ResolveLibDir(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        var fromBase = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "lib"));
        if (Directory.Exists(fromBase))
            return fromBase;

        var fromCwd = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "lib"));
        if (Directory.Exists(fromCwd))
            return fromCwd;

        var fromRepo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "lib"));
        return Directory.Exists(fromRepo) ? fromRepo : fromBase;
    }

    /// <summary>JVM user.dir — parent of lib/ (verifier.jar, token file paths).</summary>
    public static string ResolveJvmWorkingDirectory(string? configuredPath = null, string? libDir = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        if (!string.IsNullOrWhiteSpace(libDir))
        {
            var workingDir = ResolveWorkingDirFromLib(libDir);
            if (!string.IsNullOrWhiteSpace(workingDir))
                return workingDir;
        }

        return AppContext.BaseDirectory;
    }

    private static string ResolveWorkingDirFromLib(string libDir)
    {
        var path = Path.GetFullPath(libDir);
        while (path.Length > 1 && (path.EndsWith('\\') || path.EndsWith('/')))
            path = path[..^1];

        var leaf = Path.GetFileName(path);
        if (string.Equals(leaf, "lib", StringComparison.OrdinalIgnoreCase))
            return Path.GetDirectoryName(path) ?? path;

        return path;
    }

    public static string ResolveClassesDir(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        var fromBase = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "bridge", "classes"));
        if (Directory.Exists(fromBase))
            return fromBase;

        var fromCwd = Path.GetFullPath(Path.Combine(
            Environment.CurrentDirectory,
            "src",
            "DarkBot.Net.Agent.Bridge",
            "build",
            "classes"));

        if (Directory.Exists(fromCwd))
            return fromCwd;

        var fromRepo = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "DarkBot.Net.Agent.Bridge",
            "build",
            "classes"));

        return Directory.Exists(fromRepo) ? fromRepo : fromBase;
    }

    /// <summary>
    /// KekkaPlayer.dll binds natives against the signed release jar (~5 MB), not Gradle fat jars (~12 MB).
    /// </summary>
    private const long DevFatJarSizeThresholdBytes = 8 * 1024 * 1024;

    private static readonly string[] PreferredDarkBotJarSources =
    [
        @"C:\DarkBot\DarkBot.jar",
    ];

    /// <summary>verifier.jar AuthAPIImpl references classes from DarkBot.jar (e.g. Main).</summary>
    public static string? ResolveDarkBotJar(string libDir, string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var configured = Path.GetFullPath(configuredPath);
            if (File.Exists(configured))
                return configured;
        }

        foreach (var preferred in PreferredDarkBotJarSources)
        {
            if (File.Exists(preferred))
                return preferred;
        }

        var inLib = Path.Combine(libDir, "DarkBot.jar");
        if (File.Exists(inLib) && !LooksLikeDevFatJar(inLib))
            return inLib;

        var repoLib = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "lib", "DarkBot.jar"));
        if (File.Exists(repoLib) && !LooksLikeDevFatJar(repoLib))
            return repoLib;

        if (File.Exists(inLib))
            return inLib;

        var buildLibs = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "DarkBot",
            "build",
            "libs"));

        if (Directory.Exists(buildLibs))
        {
            var builtJar = Directory
                .EnumerateFiles(buildLibs, "DarkBot-*.jar")
                .Where(path => !path.Contains("sources", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (builtJar is not null)
                return builtJar;
        }

        return null;
    }

    public static string BuildBridgeClassPath(string classesDir, string libDir, string? darkBotJarPath = null)
    {
        var parts = new List<string>();
        var jar = ResolveDarkBotJar(libDir, darkBotJarPath);
        if (jar is not null)
            parts.Add(jar);

        parts.Add(Path.GetFullPath(classesDir));
        return string.Join(';', parts);
    }

    public static bool EnsureDarkBotJarInLib(
        string libDir,
        string? configuredPath,
        ILogger? logger = null)
    {
        Directory.CreateDirectory(libDir);
        var target = Path.Combine(libDir, "DarkBot.jar");
        if (File.Exists(target) && !LooksLikeDevFatJar(target))
            return true;

        var source = ResolveDarkBotJar(libDir, configuredPath);
        if (source is null || string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(target))
            {
                logger?.LogWarning(
                    "DarkBot.jar at {Target} looks like a Gradle dev build; KekkaPlayer needs the signed release jar (copy from C:\\DarkBot\\DarkBot.jar).",
                    target);
                return true;
            }

            logger?.LogWarning(
                "DarkBot.jar not found. Place the signed release at {Target} or set DarkBot:DarkBotJarPath.",
                target);
            return false;
        }

        File.Copy(source, target, overwrite: true);
        logger?.LogInformation("Copied DarkBot.jar from {Source} to {Target}", source, target);
        return true;
    }

    private static bool LooksLikeDevFatJar(string path)
    {
        try
        {
            return new FileInfo(path).Length > DevFatJarSizeThresholdBytes;
        }
        catch
        {
            return false;
        }
    }
}
