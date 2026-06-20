using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Backpage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotBackpage(this IServiceCollection services)
    {
        services.AddSingleton<BackpageService>();
        services.AddSingleton<Api.Managers.IBackpageApi>(sp => sp.GetRequiredService<BackpageService>());
        services.AddHostedService<BackpageBackgroundService>();
        return services;
    }
}
