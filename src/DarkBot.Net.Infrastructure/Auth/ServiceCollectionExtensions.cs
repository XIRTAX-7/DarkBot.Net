using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Infrastructure.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotBackpage(this IServiceCollection services)
    {
        services.AddSingleton<BackpageService>();
        services.AddSingleton<IBackpageApi>(sp => sp.GetRequiredService<BackpageService>());
        services.AddHostedService<BackpageBackgroundService>();
        return services;
    }

    public static IServiceCollection AddDarkBotLogin(this IServiceCollection services)
    {
        services.AddSingleton<BackpageSidecarLocator>();
        services.AddSingleton<ManualCaptchaSolver>();
        services.AddSingleton<DarkBackpageCaptchaSolver>();
        services.AddSingleton<CompositeCaptchaSolver>();
        services.AddSingleton<ICaptchaSolver>(sp => sp.GetRequiredService<CompositeCaptchaSolver>());
        services.AddSingleton<LoginService>();
        services.AddSingleton<ILoginService>(sp => sp.GetRequiredService<LoginService>());
        return services;
    }
}
