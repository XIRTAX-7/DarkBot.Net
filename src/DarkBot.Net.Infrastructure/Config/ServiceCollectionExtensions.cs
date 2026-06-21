using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Infrastructure.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotConfig(this IServiceCollection services)
    {
        services.AddSingleton<StubConfigApi>();
        services.AddSingleton<IConfigApi>(sp => sp.GetRequiredService<StubConfigApi>());
        return services;
    }
}
