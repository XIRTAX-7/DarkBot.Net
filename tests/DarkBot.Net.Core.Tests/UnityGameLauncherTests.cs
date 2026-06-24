using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

public sealed class UnityGameLauncherTests
{
    [Fact]
    public async Task StopAsync_WhenNoProcess_CompletesWithoutThrowing()
    {
        var launcher = CreateLauncher();

        var exception = await Record.ExceptionAsync(() => launcher.StopAsync());

        Assert.Null(exception);
    }

    private static UnityGameLauncher CreateLauncher()
    {
        var options = Options.Create(new GameApiOptions
        {
            UnityGameInstallPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        });

        return new UnityGameLauncher(
            options,
            new UnityProcessFinder(options, NullLogger<UnityProcessFinder>.Instance),
            new UnitySessionBootstrapStore(),
            NullLogger<UnityGameLauncher>.Instance);
    }
}
