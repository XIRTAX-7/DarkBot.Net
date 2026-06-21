using DarkBot.Net.Application.Extensions;
using DarkBot.Net.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Application.Tests;

public class HostStartupTests
{
    [Fact]
    public void AddApplication_resolves_without_circular_dependency()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddLogging();
                services.AddApplication();
                services.AddInfrastructure(ctx.Configuration);
            })
            .Build();

        host.Start();
        _ = host.Services.GetRequiredService<DarkBot.Net.Infrastructure.Game.FridaGameApi>();
        _ = host.Services.GetRequiredService<DarkBot.Net.Application.Managers.EntityManager>();
        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
