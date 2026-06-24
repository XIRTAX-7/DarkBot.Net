using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

public sealed class UnityGamePathsTests
{
    [Fact]
    public void ResolveInstallDirectory_UsesConfiguredPath_WhenExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"darkorbit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var options = new GameApiOptions { UnityGameInstallPath = tempDir };
            var resolved = UnityGamePaths.ResolveInstallDirectory(options);
            Assert.Equal(Path.GetFullPath(tempDir), resolved);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveExecutable_Throws_WhenExeMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"darkorbit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var options = new GameApiOptions { UnityGameInstallPath = tempDir };
            Assert.Throws<FileNotFoundException>(() => UnityGamePaths.ResolveExecutable(options));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveExecutable_ReturnsFullPath_WhenExeExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"darkorbit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var exePath = Path.Combine(tempDir, "DarkOrbit.exe");
        File.WriteAllText(exePath, string.Empty);

        try
        {
            var options = new GameApiOptions { UnityGameInstallPath = tempDir };
            var resolved = UnityGamePaths.ResolveExecutable(options);
            Assert.Equal(Path.GetFullPath(exePath), resolved);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
