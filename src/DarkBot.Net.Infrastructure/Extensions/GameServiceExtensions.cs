using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Extensions;

public static class GameServiceExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton<GameSessionStore>();
        services.AddSingleton<ElectronControlClient>();
        services.AddSingleton<FridaGameApi>();
        services.AddSingleton<UnityFridaSession>();
        services.AddSingleton<UnityFridaGameApi>();
        services.AddSingleton<UnityProcessFinder>();
        services.AddSingleton<UnityVuplexCookieSeeder>();
        services.AddSingleton<UnityWebGlLoginResolver>();
        services.AddSingleton<UnitySessionBootstrapStore>();
        services.AddSingleton<UnitySessionRefresher>();
        services.AddSingleton<UnityGameLauncher>();

        services.AddSingleton<IGameConnection>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GameApiOptions>>().Value;
            return options.BrowserApi == GameApiMode.UnityClient
                ? sp.GetRequiredService<UnityFridaGameApi>()
                : sp.GetRequiredService<FridaGameApi>();
        });

        services.AddSingleton<IGameBridgeStatusSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GameApiOptions>>().Value;
            return options.BrowserApi == GameApiMode.UnityClient
                ? sp.GetRequiredService<UnityFridaGameApi>()
                : sp.GetRequiredService<FridaGameApi>();
        });

        services.AddSingleton<IGameInstallerProbe>(sp => (IGameInstallerProbe)sp.GetRequiredService<IGameConnection>());

        services.AddSingleton<FridaGameStateProbe>();
        services.AddSingleton<IGameFridaProbe>(sp => sp.GetRequiredService<FridaGameStateProbe>());
        services.AddSingleton<DarkorbitClientLauncher>();
        services.AddSingleton<GameShutdownCoordinator>();
        services.AddSingleton<GameClientConnectService>();
        services.AddSingleton<GameLauncherService>();
        services.AddSingleton<IGameLauncherService>(sp => sp.GetRequiredService<GameLauncherService>());
        services.AddHostedService<FridaBridgeHostedService>();
        services.AddSingleton<GameClientLifecycle>();
        services.AddSingleton<GameLaunchSessionResolver>();
        services.AddSingleton<GameClientRestartService>();
        services.AddSingleton<IGameClientRestartAppService>(sp => sp.GetRequiredService<GameClientRestartService>());
        services.AddHostedService<GameClientRestartListener>();
        services.AddSingleton<GameClientShutdownService>();
        services.AddHostedService(sp => sp.GetRequiredService<GameClientShutdownService>());

        return services;
    }
}
