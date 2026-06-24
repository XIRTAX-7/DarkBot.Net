using DarkBot.Net.Application.Bot;
using DarkBot.Net.Application.Extensions;
using DarkBot.Net.Infrastructure;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Application.Tests;

public sealed class GameClientShutdownServiceTests
{
    [Fact]
    public async Task StopAsync_DelegatesToCoordinatorMemoizedTask()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddLogging();
                services.AddApplication();
                services.AddInfrastructure(ctx.Configuration);
            })
            .Build();

        await host.StartAsync();

        var coordinator = host.Services.GetRequiredService<GameShutdownCoordinator>();
        var shutdownService = host.Services.GetRequiredService<GameClientShutdownService>();

        var coordinatorTask = coordinator.StopGameClientAsync();
        await shutdownService.StopAsync(CancellationToken.None);

        Assert.True(coordinatorTask.IsCompletedSuccessfully);
        Assert.Same(coordinatorTask, coordinator.StopGameClientAsync());

        await host.StopAsync();
        host.Dispose();
    }
}
