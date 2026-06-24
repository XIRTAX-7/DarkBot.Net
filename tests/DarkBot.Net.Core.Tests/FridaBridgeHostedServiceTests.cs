using DarkBot.Net.Application.Tests.Helpers;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

public sealed class FridaBridgeHostedServiceTests
{
    [Fact]
    public async Task StopAsync_CompletesWhileRetryingConnection()
    {
        var options = Options.Create(new GameApiOptions
        {
            BrowserApi = GameApiMode.FridaClient,
            FridaApiPort = NetworkTestHelpers.GetFreeTcpPort()
        });

        var service = CreateService(options);
        await service.StartAsync(CancellationToken.None);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task StopAsync_CompletesWhileConnectedToWebSocket()
    {
        await using var server = new TestWebSocketServer();
        await server.StartAsync();

        var options = Options.Create(new GameApiOptions
        {
            BrowserApi = GameApiMode.FridaClient,
            FridaApiPort = server.Port
        });
        var service = CreateService(options);

        await service.StartAsync(CancellationToken.None);
        await AsyncWaitHelpers.WaitUntilAsync(
            () => server.ConnectionCount > 0,
            TimeSpan.FromSeconds(5));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3));
    }

    private static FridaBridgeHostedService CreateService(IOptions<GameApiOptions> options)
    {
        var control = new ElectronControlClient(options, NullLogger<ElectronControlClient>.Instance);
        var frida = new FridaGameApi(
            control,
            options,
            NullLogger<FridaGameApi>.Instance);

        return new FridaBridgeHostedService(frida, options, NullLogger<FridaBridgeHostedService>.Instance);
    }
}
