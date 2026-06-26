using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Hosting;
using DarkBot.Net.Presentation.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Presentation.Tests;

public sealed class VerifierSidecarHostedServiceTests
{
    [Fact]
    public async Task StopAsync_DevStubLoopExits()
    {
        var port = NetworkTestHelpers.GetFreeTcpPort();
        var options = Options.Create(new DarkBotUiOptions
        {
            VerifierDevBypass = true,
            VerifierPort = port
        });

        var service = new VerifierSidecarHostedService(options, NullLogger<VerifierSidecarHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await AsyncWaitHelpers.WaitUntilAsync(() => service.IsListening, TimeSpan.FromSeconds(3));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3));
        Assert.False(service.IsListening);
    }

    [Fact]
    public async Task StopAsync_WhenDevBypassDisabledAndNoJar_CompletesQuickly()
    {
        var options = Options.Create(new DarkBotUiOptions
        {
            VerifierDevBypass = false,
            VerifierPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.jar")
        });

        var service = new VerifierSidecarHostedService(options, NullLogger<VerifierSidecarHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }
}
