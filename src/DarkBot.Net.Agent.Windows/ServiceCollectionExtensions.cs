using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Api.Game;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Agent.Windows;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotAgent(this IServiceCollection services)
    {
        services.AddSingleton<GameSessionStore>();
        services.AddSingleton<ElectronControlClient>();
        services.AddSingleton<GamePacketReader>();
        services.AddSingleton<FridaGameApi>();
        services.AddSingleton<IGameConnection>(sp => sp.GetRequiredService<FridaGameApi>());
        services.AddSingleton<FridaGameStateProbe>();
        services.AddSingleton<IGameFridaProbe>(sp => sp.GetRequiredService<FridaGameStateProbe>());
        services.AddSingleton<DarkorbitClientLauncher>();
        services.AddSingleton<GameClientConnectService>();
        services.AddSingleton<GameLauncherService>();
        services.AddSingleton<GameReloginService>();
        services.AddHostedService<GamePacketBridgeHostedService>();
        services.AddHostedService<FridaBridgeHostedService>();
        services.AddSingleton<GameClientShutdownService>();
        services.AddHostedService(sp => sp.GetRequiredService<GameClientShutdownService>());

        return services;
    }
}
