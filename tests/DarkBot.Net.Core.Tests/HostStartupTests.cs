using DarkBot.Net.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Core.Tests;

public class HostStartupTests
{
    [Fact]
    public void AddDarkBotCore_resolves_without_circular_dependency()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddDarkBotCore();
            })
            .Build();

        host.Start();
        _ = host.Services.GetRequiredService<DarkBot.Net.Agent.Windows.Game.FridaGameApi>();
        _ = host.Services.GetRequiredService<DarkBot.Net.Core.Managers.EntityManager>();
        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
