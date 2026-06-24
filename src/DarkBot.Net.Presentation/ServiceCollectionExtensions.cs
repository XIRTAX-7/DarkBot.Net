using DarkBot.Net.Application.Extensions;
using DarkBot.Net.Infrastructure;
using DarkBot.Net.Presentation.Configuration;
using DarkBot.Net.Presentation.Logging;
using DarkBot.Net.Presentation.Services;
using DarkBot.Net.Presentation.Services.Shell;
using DarkBot.Net.Presentation.ViewModels;
using DarkBot.Net.Presentation.ViewModels.Shell;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Presentation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationUi(this IServiceCollection services)
    {
        services.Configure<DarkBotUiOptions>(Program.Configuration.GetSection(DarkBotUiOptions.SectionName));
        services.Configure<TestLoginOptions>(Program.Configuration.GetSection(TestLoginOptions.SectionName));
        services.Configure<GameApiOptions>(Program.Configuration.GetSection(GameApiOptions.SectionName));
        services.PostConfigure<GameApiOptions>(options =>
        {
            var ui = Program.Configuration.GetSection(DarkBotUiOptions.SectionName).Get<DarkBotUiOptions>();
            if (ui is null)
            {
                return;
            }

            options.LibPath = ui.LibPath;
            options.ClassesPath = ui.ClassesPath;
            options.DarkBotJarPath = ui.DarkBotJarPath;
            options.BrowserApi = ui.BrowserApi;
            options.Width = ui.GameWidth;
            options.Height = ui.GameHeight;
            options.Use3D = ui.Use3D;
            options.UseProxy = ui.UseProxy;
            options.ForceGameLanguage = ui.ForceGameLanguage;
            options.GameLanguage = ui.GameLanguage;
            options.FridaApiPort = ui.FridaApiPort;
            options.DarkorbitClientPath = ui.DarkorbitClientPath;
            options.ClientConnectTimeoutSec = 180;
            options.FridaReadyTimeoutSec = 180;
        });

        services.AddSingleton<BotUiStateService>();
        services.AddSingleton<GameConnectionStatusService>();

        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ConfigTreeViewModel>();
        services.AddSingleton<StatsPanelViewModel>();
        services.AddSingleton<ShellWindowViewModel>();

        services.AddSingleton<IShellWindowService, ShellWindowService>();

        services.AddSingleton<VerifierSidecarHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<VerifierSidecarHostedService>());
        services.AddSingleton<GameAutoLaunchService>();
        services.AddHostedService(sp => sp.GetRequiredService<GameAutoLaunchService>());

        return services;
    }

    public static IHost BuildDarkBotHost(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config => config.AddConfiguration(configuration))
            .UseDarkBotSerilog(configuration)
            .ConfigureServices(services =>
            {
                services.Configure<HostOptions>(options =>
                    options.ShutdownTimeout = TimeSpan.FromSeconds(10));

                services.AddApplication();
                services.AddInfrastructure(configuration);
                services.AddPresentationUi();
            })
            .Build();
}
