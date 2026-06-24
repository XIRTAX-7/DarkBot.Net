using DarkBot.Net.Core.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Пути к Unity-клиенту DarkOrbit (C:\DarkOrbit_Version1.1.102).</summary>
public static class UnityGamePaths
{
    public const string DefaultExecutableName = "DarkOrbit.exe";

    public static string ResolveInstallDirectory(GameApiOptions options)
    {
        var configured = string.IsNullOrWhiteSpace(options.UnityGameInstallPath)
            ? @"C:\DarkOrbit_Version1.1.102"
            : options.UnityGameInstallPath.Trim();

        var full = Path.GetFullPath(configured);
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"Unity game install directory not found: {full}");

        return full;
    }

    public static string ResolveExecutable(GameApiOptions options)
    {
        var installDir = ResolveInstallDirectory(options);
        var exeName = string.IsNullOrWhiteSpace(options.UnityGameExecutableName)
            ? DefaultExecutableName
            : options.UnityGameExecutableName.Trim();

        var exePath = Path.Combine(installDir, exeName);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Unity game executable not found: {exePath}", exePath);

        return exePath;
    }
}
