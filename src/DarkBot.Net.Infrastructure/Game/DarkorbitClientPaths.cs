namespace DarkBot.Net.Infrastructure.Game;

public static class DarkorbitClientPaths
{
    public static string Resolve(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var full = Path.GetFullPath(configuredPath);
            if (IsClientRoot(full))
                return full;
        }

        var env = Environment.GetEnvironmentVariable("DARKORBIT_CLIENT_PATH");
        if (!string.IsNullOrWhiteSpace(env) && IsClientRoot(env))
            return Path.GetFullPath(env);

        string? fallback = null;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Darkorbit-client");
            if (!IsClientRoot(candidate))
                continue;

            if (HasNpmDependencies(candidate))
                return candidate;

            fallback ??= candidate;
        }

        if (fallback is not null)
            return fallback;

        throw new DirectoryNotFoundException(
            "Darkorbit-client not found. Set DarkBot:DarkorbitClientPath or DARKORBIT_CLIENT_PATH.");
    }

    public static bool HasNpmDependencies(string clientRoot) =>
        Directory.Exists(Path.Combine(clientRoot, "node_modules", "electron"));

    public static bool IsClientRoot(string path) =>
        File.Exists(Path.Combine(path, "package.json"))
        && File.Exists(Path.Combine(path, "index.js"));

    public static string ResolveElectronExecutable(string clientRoot)
    {
        if (OperatingSystem.IsWindows())
        {
            var distExe = Path.Combine(clientRoot, "node_modules", "electron", "dist", "electron.exe");
            if (File.Exists(distExe))
                return distExe;
        }
        else if (OperatingSystem.IsLinux())
        {
            var distBin = Path.Combine(clientRoot, "node_modules", "electron", "dist", "electron");
            if (File.Exists(distBin))
                return distBin;
        }
        else if (OperatingSystem.IsMacOS())
        {
            var macApp = Path.Combine(
                clientRoot, "node_modules", "electron", "dist", "Electron.app", "Contents", "MacOS", "Electron");
            if (File.Exists(macApp))
                return macApp;
        }

        var win = Path.Combine(clientRoot, "node_modules", ".bin", "electron.cmd");
        if (OperatingSystem.IsWindows() && File.Exists(win))
            return win;

        var unix = Path.Combine(clientRoot, "node_modules", ".bin", "electron");
        if (File.Exists(unix))
            return unix;

        throw new FileNotFoundException(
            "Electron not installed in Darkorbit-client. Run: npm install",
            win);
    }

    public static string ResolveElectronSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        foreach (var folder in new[] { "DarkOrbit Client", "darkorbit-client", "Darkorbit-client" })
        {
            var path = Path.Combine(appData, folder, "settings.json");
            if (File.Exists(path))
                return path;
        }

        return Path.Combine(appData, "DarkOrbit Client", "settings.json");
    }
}
