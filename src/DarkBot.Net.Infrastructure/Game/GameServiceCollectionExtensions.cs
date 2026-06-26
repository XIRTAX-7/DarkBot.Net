using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Infrastructure.Game;

public static class GameServiceCollectionExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddSingleton<GameSessionStore>();
        services.AddSingleton<IGameSessionStore>(sp => sp.GetRequiredService<GameSessionStore>());
        services.AddSingleton<UnityFridaSession>();
        services.AddSingleton<UnityFridaGameApi>();
        services.AddSingleton<UnityProcessFinder>();
        services.AddSingleton<UnitySessionBootstrapStore>();
        services.AddSingleton<UnitySessionRefresher>();
        services.AddSingleton<UnityGameLauncher>();

        services.AddSingleton<IGameConnection>(sp => sp.GetRequiredService<UnityFridaGameApi>());
        services.AddSingleton<IGameBridgeStatusSource>(sp => sp.GetRequiredService<UnityFridaGameApi>());
        services.AddSingleton<IGameBridgePhaseSource>(sp => sp.GetRequiredService<UnityFridaGameApi>());
        services.AddSingleton<IGameInstallerProbe>(sp => sp.GetRequiredService<UnityFridaGameApi>());

        services.AddSingleton<UnityBridgeStateProbe>();
        services.AddSingleton<IGameFridaProbe>(sp => sp.GetRequiredService<UnityBridgeStateProbe>());
        services.AddSingleton<GameShutdownCoordinator>();
        services.AddSingleton<GameClientConnectService>();
        services.AddSingleton<GameLauncherService>();
        services.AddSingleton<IGameLauncherService>(sp => sp.GetRequiredService<GameLauncherService>());
        services.AddSingleton<GameClientLifecycle>();
        services.AddSingleton<GameLaunchSessionResolver>();
        services.AddSingleton<GameClientRestartService>();
        services.AddSingleton<IGameClientRestartAppService>(sp => sp.GetRequiredService<GameClientRestartService>());
        services.AddSingleton<GameShutdownAppService>();
        services.AddSingleton<IGameShutdownAppService>(sp => sp.GetRequiredService<GameShutdownAppService>());
        services.AddHostedService<GameClientRestartListener>();
        services.AddSingleton<GameClientShutdownService>();
        services.AddHostedService(sp => sp.GetRequiredService<GameClientShutdownService>());

        return services;
    }
}
