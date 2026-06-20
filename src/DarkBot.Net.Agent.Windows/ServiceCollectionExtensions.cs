using DarkBot.Net.Agent.Windows.Bridge;
using DarkBot.Net.Agent.Windows.Game;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Agent.Windows;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotAgent(this IServiceCollection services)
    {
        services.AddSingleton<NativeGameBridge>();
        services.AddSingleton<GameSessionStore>();
        services.AddSingleton<NativeLibrarySetup>();
        services.AddSingleton<FridaGameApi>();
        services.AddSingleton<IGameConnection>(sp => sp.GetRequiredService<FridaGameApi>());
        services.AddSingleton<DarkorbitClientLauncher>();
        services.AddSingleton<GameClientConnectService>();
        services.AddSingleton<GameLauncherService>();
        services.AddSingleton<GameReloginService>();

        return services;
    }
}
