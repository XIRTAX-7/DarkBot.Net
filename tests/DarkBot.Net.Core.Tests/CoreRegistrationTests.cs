using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Application.Tests;

public class CoreRegistrationTests
{
    [Fact]
    public void AddApplication_registers_managers_and_bot_loop()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        Assert.Contains(services, d => d.ServiceType == typeof(BotLoopService));
        Assert.Contains(services, d => d.ServiceType == typeof(HeroManager));
        Assert.Contains(services, d => d.ServiceType == typeof(MapManager));
        Assert.Contains(services, d => d.ServiceType == typeof(StatsManager));
    }
}
