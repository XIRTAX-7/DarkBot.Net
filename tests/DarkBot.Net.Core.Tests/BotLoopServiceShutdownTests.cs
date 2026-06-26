using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Install;
using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.BotEngine.Runtime;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public sealed class BotLoopServiceShutdownTests
{
    [Fact]
    public void Stop_FromConcurrentThread_DoesNotThrow()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSingleton<BotAddressRegistry>();
                services.AddSingleton<FakeGameConnection>();
                services.AddSingleton<IGameConnection>(sp => sp.GetRequiredService<FakeGameConnection>());
                services.AddSingleton<IGameFridaProbe, NullGameFridaProbe>();
                services.AddSingleton<BotModuleRunner>();
                services.AddSingleton<BotInstallerService>();
                services.AddSingleton<StarManager>();
                services.AddSingleton<HeroManager>();
                services.AddSingleton<MapManager>();
                services.AddSingleton<EntityManager>();
                services.AddSingleton<StatsManager>();
                services.AddSingleton<MovementApi>();
                services.AddSingleton<EntitiesApi>();
                services.AddSingleton<RepairApi>();
                services.AddSingleton<I18nApi>();
                services.AddSingleton<OreApi>();
                services.AddSingleton<StarSystemApi>();
                services.AddSingleton<BotRuntime>();
                services.AddSingleton<BotLoopService>();
            })
            .Build();

        host.Start();

        var loop = host.Services.GetRequiredService<BotLoopService>();
        loop.Start();

        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                loop.Stop();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.Start();
        thread.Join(TimeSpan.FromSeconds(2));

        Assert.Null(captured);
        Assert.False(loop.IsRunning);

        host.StopAsync().GetAwaiter().GetResult();
        host.Dispose();
    }
}
