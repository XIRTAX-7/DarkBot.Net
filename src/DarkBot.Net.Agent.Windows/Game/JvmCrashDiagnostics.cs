namespace DarkBot.Net.Agent.Windows.Game;

internal static class JvmCrashDiagnostics
{
    public static string? FindRecentCrashReport(string baseDirectory, TimeSpan maxAge)
    {
        if (!Directory.Exists(baseDirectory))
            return null;

        var cutoff = DateTime.Now - maxAge;
        var latest = Directory.EnumerateFiles(baseDirectory, "hs_err_pid*.log")
            .Select(path => new FileInfo(path))
            .Where(info => info.LastWriteTime >= cutoff)
            .OrderByDescending(info => info.LastWriteTime)
            .FirstOrDefault();

        if (latest is null)
            return null;

        foreach (var line in File.ReadLines(latest.FullName))
        {
            if (line.StartsWith("#  EXCEPTION_", StringComparison.Ordinal))
            {
                return
                    $"JVM crashed in KekkaPlayer native code ({line.TrimStart('#', ' ')}). " +
                    $"See {latest.FullName}. This usually means Flash ActiveX failed to initialize — " +
                    "verify DarkFlash.ocx (regsvr32) and that Java DarkBot works on this machine.";
            }
        }

        return $"JVM crash report written to {latest.FullName}";
    }
}
