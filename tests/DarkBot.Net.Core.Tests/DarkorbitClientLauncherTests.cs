using DarkBot.Net.Application.Tests.Helpers;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

public sealed class DarkorbitClientLauncherTests
{
    [Fact]
    public async Task StopAsync_WhenNoProcess_CompletesWithoutThrowing()
    {
        var launcher = CreateLauncher();

        var exception = await Record.ExceptionAsync(() => launcher.StopAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_ConcurrentCalls_AllComplete()
    {
        var launcher = CreateLauncher();

        var tasks = Enumerable.Range(0, 4)
            .Select(_ => launcher.StopAsync())
            .ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task StopAsync_WhenClientRootMissing_DoesNotThrow()
    {
        var options = Options.Create(new GameApiOptions
        {
            DarkorbitClientPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        });

        var launcher = new DarkorbitClientLauncher(options, NullLogger<DarkorbitClientLauncher>.Instance);

        var exception = await Record.ExceptionAsync(() => launcher.StopAsync());

        Assert.Null(exception);
    }

    private static DarkorbitClientLauncher CreateLauncher()
    {
        var options = Options.Create(new GameApiOptions());
        return new DarkorbitClientLauncher(options, NullLogger<DarkorbitClientLauncher>.Instance);
    }
}
