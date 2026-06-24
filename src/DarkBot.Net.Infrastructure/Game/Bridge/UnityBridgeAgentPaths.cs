using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Резолвит путь к Unity Frida bridge agent в репозитории или по конфигу.</summary>
public static class UnityBridgeAgentPaths
{
    public const string DefaultRelativePath = "darkorbit-unity-bridge/agent/dist/agent.js";
    public const string LegacyTsRelativePath = "DarkOrbit_Version_TS/agent/dist/agent.js";
    public const string LegacyRelativePath = "DarkOrbit_Version1.1.102/unity_bridge_agent.js";

    public static string Resolve(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var full = Path.GetFullPath(configuredPath);
            if (!File.Exists(full))
                throw new FileNotFoundException($"Unity bridge agent not found: {full}", full);

            return full;
        }

        foreach (var candidate in EnumerateDefaultCandidates())
        {
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
        }

        throw new FileNotFoundException(
            $"Unity bridge agent not found. Set DarkBot:UnityBridgeAgentPath or place {DefaultRelativePath} under DarkBot.Net.");
    }

    public static IEnumerable<string> EnumerateDefaultCandidates()
    {
        foreach (var relativePath in new[] { DefaultRelativePath, LegacyTsRelativePath, LegacyRelativePath })
        {
            var baseDir = AppContext.BaseDirectory;
            yield return Path.Combine(baseDir, relativePath);
            yield return Path.Combine(baseDir, "..", "..", "..", relativePath);
            yield return Path.Combine(baseDir, "..", "..", "..", "..", relativePath);
            yield return Path.Combine(baseDir, "..", "..", "..", "..", "..", relativePath);

            var cwd = Directory.GetCurrentDirectory();
            yield return Path.Combine(cwd, "DarkBot.Net", relativePath);
            yield return Path.Combine(cwd, relativePath);

            var repoRoot = FindRepoRoot(baseDir) ?? FindRepoRoot(cwd);
            if (repoRoot is not null)
                yield return Path.Combine(repoRoot, "DarkBot.Net", relativePath);
        }
    }

    private static string? FindRepoRoot(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "DarkBot.Net", "darkorbit-unity-bridge"))
                || Directory.Exists(Path.Combine(current.FullName, "DarkBot.Net", "DarkOrbit_Version_TS"))
                || Directory.Exists(Path.Combine(current.FullName, "DarkBot.Net", "DarkOrbit_Version1.1.102")))
                return current.FullName;

            current = current.Parent;
        }

        return null;
    }
}
