using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Infrastructure.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotConfig(this IServiceCollection services)
    {
        services.AddSingleton<IConfigPersistence, JsonConfigPersistence>();
        services.AddSingleton<IConfigWritePolicy, ConfigWritePolicy>();
        services.AddSingleton<JsonConfigApi>();
        services.AddSingleton<IConfigApi>(sp => sp.GetRequiredService<JsonConfigApi>());
        services.AddSingleton<ConfigSession>();
        services.AddSingleton<IConfigSession>(sp => sp.GetRequiredService<ConfigSession>());
        services.AddHostedService<ConfigSessionInitializer>();

        return services;
    }
}

internal sealed class ConfigSessionInitializer(ConfigSession session) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        session.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
