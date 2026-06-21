using DarkBot.Net.Application.Bot;
using DarkBot.Net.Application.Managers;
using DarkBot.Net.Application.Memory;
using DarkBot.Net.Application.Services.Auth;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAppServices();

        services.AddSingleton<BotAddressRegistry>();
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

        services.AddSingleton<BotModuleRunner>();
        services.AddSingleton<BotRuntime>();
        services.AddSingleton<BotInstallerService>();
        services.AddHostedService(sp => sp.GetRequiredService<BotInstallerService>());
        services.AddSingleton<BotLoopService>();
        services.AddSingleton<IBotController>(sp => sp.GetRequiredService<BotLoopService>());
        services.AddHostedService(sp => sp.GetRequiredService<BotLoopService>());

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<LoginAppService>()
            .AddClasses(classes => classes
                .Where(type => type.IsClass
                    && !type.IsAbstract
                    && type.Name.EndsWith("AppService", StringComparison.Ordinal)))
            .AsMatchingInterface()
            .WithSingletonLifetime());

        return services;
    }
}
