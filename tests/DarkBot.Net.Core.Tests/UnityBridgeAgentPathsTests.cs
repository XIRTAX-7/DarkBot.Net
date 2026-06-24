using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Application.Tests;

public sealed class UnityBridgeAgentPathsTests
{
    [Fact]
    public void Resolve_UsesConfiguredPath_WhenFileExists()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"unity-bridge-{Guid.NewGuid():N}.js");
        File.WriteAllText(tempFile, "// test agent");

        try
        {
            var resolved = UnityBridgeAgentPaths.Resolve(tempFile);
            Assert.Equal(Path.GetFullPath(tempFile), resolved);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Resolve_Throws_WhenConfiguredPathMissing()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.js");
        Assert.Throws<FileNotFoundException>(() => UnityBridgeAgentPaths.Resolve(missing));
    }

    [Fact]
    public void EnumerateDefaultCandidates_IncludesRepoRelativePath()
    {
        var candidates = UnityBridgeAgentPaths.EnumerateDefaultCandidates().ToList();
        Assert.Contains(candidates, path => path.Contains("DarkOrbit_Version1.1.102", StringComparison.OrdinalIgnoreCase));
    }
}
