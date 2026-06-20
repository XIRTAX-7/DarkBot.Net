using DarkBot.Net.Agent.Windows;
using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Agent.Windows.Memory;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Bot;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;
using DarkBot.Net.Plugins;
using DarkBot.Net.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotCore(this IServiceCollection services)
    {
        services.AddDarkBotPlugins();
        services.AddDarkBotAgent();

        services.AddSingleton<BotAddressRegistry>();
        services.AddSingleton<IGameMemoryAccess, GameMemoryAccess>();
        services.AddSingleton(sp =>
        {
            var registry = sp.GetRequiredService<BotAddressRegistry>();
            var game = sp.GetRequiredService<IGameConnection>();
            return new ExtraMemoryReader(game, () => registry.MainApplicationAddress);
        });

        services.AddSingleton<StarManager>();
        services.AddSingleton<HeroManager>();
        services.AddSingleton<IHeroApi>(sp => sp.GetRequiredService<HeroManager>());
        services.AddSingleton<MapManager>();
        services.AddSingleton<EntityManager>();
        services.AddSingleton<StatsManager>();
        services.AddSingleton<IStatsApi>(sp => sp.GetRequiredService<StatsManager>());

        services.AddSingleton<MovementApi>();
        services.AddSingleton<IMovementApi>(sp => sp.GetRequiredService<MovementApi>());
        services.AddSingleton<GameDirectApi>();
        services.AddSingleton<EntitiesApi>();
        services.AddSingleton<IEntitiesApi>(sp => sp.GetRequiredService<EntitiesApi>());
        services.AddSingleton<RepairApi>();
        services.AddSingleton<IRepairApi>(sp => sp.GetRequiredService<RepairApi>());
        services.AddSingleton<I18nApi>();
        services.AddSingleton<II18nApi>(sp => sp.GetRequiredService<I18nApi>());
        services.AddSingleton<OreApi>();
        services.AddSingleton<IOreApi>(sp => sp.GetRequiredService<OreApi>());
        services.AddSingleton<StarSystemApi>();
        services.AddSingleton<IStarSystemApi>(sp => sp.GetRequiredService<StarSystemApi>());
        services.AddSingleton<LegacyModuleApi>();
        services.AddSingleton<ILegacyModuleApi>(sp => sp.GetRequiredService<LegacyModuleApi>());

        services.AddSingleton<ModuleController>();
        services.AddSingleton<BotApi>();
        services.AddSingleton<IBotApi>(sp => sp.GetRequiredService<BotApi>());

        services.AddSingleton<BotRuntime>();
        services.AddSingleton<BotInstallerService>();
        services.AddHostedService(sp => sp.GetRequiredService<BotInstallerService>());
        services.AddSingleton<BotLoopService>();
        services.AddSingleton<IBotController>(sp => sp.GetRequiredService<BotLoopService>());
        services.AddHostedService(sp => sp.GetRequiredService<BotLoopService>());
        services.AddSingleton<NativeGameBridgeShutdownService>();
        services.AddHostedService(sp => sp.GetRequiredService<NativeGameBridgeShutdownService>());

        return services;
    }
}
