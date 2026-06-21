using DarkBot.Net.Infrastructure.Auth;
using DarkBot.Net.Infrastructure.Config;
using DarkBot.Net.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGameServices();
        services.AddDarkBotBackpage();
        services.AddDarkBotConfig();
        services.AddDarkBotLogin();

        return services;
    }
}
