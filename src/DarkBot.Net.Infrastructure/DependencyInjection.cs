using DarkBot.Net.Infrastructure.Auth;
using DarkBot.Net.Infrastructure.Config;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddGameServices();
        services.AddDarkBotCredentials();
        services.AddDarkBotConfig();

        return services;
    }
}
