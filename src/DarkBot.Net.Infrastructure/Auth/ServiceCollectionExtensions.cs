using DarkBot.Net.Core.Interfaces.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Infrastructure.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotCredentials(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
            services.AddSingleton<ICredentialStore, WindowsCredentialStore>();
        else
            services.AddSingleton<ICredentialStore, NullCredentialStore>();

        return services;
    }
}
