using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Auth;
using DarkBot.Net.Infrastructure.Config;
using DarkBot.Net.Infrastructure.Game;
using DarkBot.Net.Infrastructure.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DarkBotUiOptions>(configuration.GetSection(DarkBotUiOptions.SectionName));

        services.AddGameServices();
        services.AddDarkBotCredentials();
        services.AddDarkBotConfig();

        services.AddSingleton<VerifierSidecarHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<VerifierSidecarHostedService>());

        return services;
    }
}
