using DarkBot.Net.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Core.Tests;

public class CoreRegistrationTests
{
    [Fact]
    public void AddDarkBotCore_registers_managers_and_bot_loop()
    {
        var services = new ServiceCollection();
        services.AddDarkBotCore();

        Assert.Same(services, services);
        Assert.Contains(services, d => d.ServiceType == typeof(Bot.BotLoopService));
        Assert.Contains(services, d => d.ServiceType == typeof(Managers.HeroManager));
        Assert.Contains(services, d => d.ServiceType == typeof(Managers.MapManager));
        Assert.Contains(services, d => d.ServiceType == typeof(Managers.StatsManager));
    }
}
