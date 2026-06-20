namespace DarkBot.Net.Agent.Windows.Game;

public static class KekkaPlayerLogReader
{
    private static readonly string[] InterestingLevels = ["ERROR", "WARN", "FATAL", "SEVERE"];

    public static IReadOnlyList<string> ReadRecentDiagnostics(string logsDirectory, TimeSpan maxAge)
    {
        if (!Directory.Exists(logsDirectory))
            return Array.Empty<string>();

        var cutoff = DateTime.Now - maxAge;
        var latestLog = Directory.EnumerateFiles(logsDirectory, "*_KekkaPlayer.log")
            .Select(path => new FileInfo(path))
            .Where(info => info.LastWriteTime >= cutoff)
            .OrderByDescending(info => info.LastWriteTime)
            .FirstOrDefault();

        if (latestLog is null)
            return Array.Empty<string>();

        var lines = File.ReadAllLines(latestLog.FullName);
        var messageLoopIndex = Array.FindIndex(
            lines,
            static line => line.Contains("Starting new message loop", StringComparison.OrdinalIgnoreCase));

        var startIndex = messageLoopIndex >= 0 ? messageLoopIndex + 1 : 0;
        var results = new List<string>();

        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
                continue;

            if (InterestingLevels.Any(level => line.Contains($"| {level}]", StringComparison.OrdinalIgnoreCase)))
                results.Add(line);
        }

        if (results.Count == 0 && messageLoopIndex >= 0 && lines.Length <= messageLoopIndex + 1)
        {
            results.Add(
                $"KekkaPlayer log {latestLog.Name} contains no lines after \"Starting new message loop\" — " +
                "the Flash message loop exited immediately without reporting a reason.");
        }

        return results;
    }

    public static string? FindLatestLogPath(string logsDirectory)
    {
        if (!Directory.Exists(logsDirectory))
            return null;

        return Directory.EnumerateFiles(logsDirectory, "*_KekkaPlayer.log")
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }
}
