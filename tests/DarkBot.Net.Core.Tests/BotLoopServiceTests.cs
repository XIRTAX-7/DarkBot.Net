using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Install;
using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.BotEngine.Runtime;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Interfaces.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public class BotLoopServiceTests
{
    [Fact]
    public void BotInstaller_skips_internet_read_before_game_launch()
    {
        var addresses = new BotAddressRegistry();
        var game = new FakeGameConnection
        {
            IsLaunched = false,
            ThrowOnLastInternetReadTime = true
        };
        var installer = new BotInstallerService(
            addresses,
            game,
            NullLogger<BotInstallerService>.Instance);

        var exception = Record.Exception(installer.Tick);

        Assert.Null(exception);
        Assert.Equal(0, game.LastInternetReadTimeCallCount);
        Assert.True(addresses.IsInvalid);
    }

    [Fact]
    public async Task BotLoop_ticks_when_screen_manager_is_valid()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSingleton<BotAddressRegistry>();
                services.AddSingleton<FakeGameConnection>();
                services.AddSingleton<IGameConnection>(sp => sp.GetRequiredService<FakeGameConnection>());
                services.AddSingleton<IUnityGameBridge>(sp => sp.GetRequiredService<FakeGameConnection>());
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
                services.AddHostedService(sp => sp.GetRequiredService<BotLoopService>());
            })
            .Build();

        await host.StartAsync();

        var addresses = host.Services.GetRequiredService<BotAddressRegistry>();
        addresses.SetScreenManagerAddress(0x1000);

        var loop = host.Services.GetRequiredService<BotLoopService>();
        loop.Start();
        await Task.Delay(150);
        loop.Pause();

        await host.StopAsync();
        host.Dispose();
    }
}
