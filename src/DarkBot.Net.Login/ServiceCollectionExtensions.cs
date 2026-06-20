using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Login;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotLogin(this IServiceCollection services)
    {
        services.AddSingleton<BackpageSidecarLocator>();
        services.AddSingleton<ManualCaptchaSolver>();
        services.AddSingleton<DarkBackpageCaptchaSolver>();
        services.AddSingleton<ICaptchaSolver, CompositeCaptchaSolver>();
        services.AddSingleton<LoginService>();
        return services;
    }
}
