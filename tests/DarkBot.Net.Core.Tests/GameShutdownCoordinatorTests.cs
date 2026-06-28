using DarkBot.Net.Core.Interfaces.Bot;
using DarkBot.Net.Application.Extensions;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

public sealed class GameShutdownCoordinatorTests
{
    [Fact]
    public async Task StopGameClientAsync_ConcurrentCalls_ReturnSameTaskAndStopBotOnce()
    {
        var bot = new CountingBotController();
        var coordinator = CreateCoordinator(bot);

        var first = coordinator.StopGameClientAsync();
        var second = coordinator.StopGameClientAsync();

        Assert.Same(first, second);
        await Task.WhenAll(first, second);
        Assert.Equal(1, bot.StopCount);
    }

    [Fact]
    public async Task StopGameClientAsync_BotStopFailure_StillCompletesShutdown()
    {
        var coordinator = CreateCoordinator(new ThrowingBotController());
        var exception = await Record.ExceptionAsync(() => coordinator.StopGameClientAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task StopGameClientAsync_SecondCallAfterCompletion_ReturnsSameCompletedTask()
    {
        var bot = new CountingBotController();
        var coordinator = CreateCoordinator(bot);

        var first = coordinator.StopGameClientAsync();
        await first;

        var second = coordinator.StopGameClientAsync();
        Assert.Same(first, second);
        Assert.True(second.IsCompletedSuccessfully);
        Assert.Equal(1, bot.StopCount);
    }

    [Fact]
    public async Task Host_stop_completes_within_timeout()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddLogging();
                services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(5));
                services.AddApplication();
                services.AddInfrastructure();
            })
            .Build();

        host.Start();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await host.StopAsync(timeout.Token);
        host.Dispose();
    }

    private static GameShutdownCoordinator CreateCoordinator(IBotController bot)
    {
        var options = Options.Create(new GameApiOptions());
        var unitySession = new UnityFridaSession(options, NullLogger<UnityFridaSession>.Instance);
        var unityFrida = new UnityFridaGameApi(
            unitySession,
            options,
            NullLogger<UnityFridaGameApi>.Instance,
            new ServiceCollection().BuildServiceProvider());
        var unityLauncher = new UnityGameLauncher(
            options,
            new UnityProcessFinder(options, NullLogger<UnityProcessFinder>.Instance),
            new UnitySessionBootstrapStore(),
            NullLogger<UnityGameLauncher>.Instance);

        return new GameShutdownCoordinator(
            unityLauncher,
            bot,
            unityFrida,
            new GameClientLifecycle(),
            NullLogger<GameShutdownCoordinator>.Instance);
    }

    private sealed class CountingBotController : IBotController
    {
        public int StopCount { get; private set; }

        public bool IsRunning => false;
        public long TickCount => 0;
        public double LastTickMs => 0;

        public double LastLoopPeriodMs => 0;

        public void Start() { }

        public void Pause() { }

        public void Stop() => StopCount++;
    }

    private sealed class ThrowingBotController : IBotController
    {
        public bool IsRunning => false;
        public long TickCount => 0;
        public double LastTickMs => 0;

        public double LastLoopPeriodMs => 0;

        public void Start() { }

        public void Pause() { }

        public void Stop() => throw new InvalidOperationException("boom");
    }
}
