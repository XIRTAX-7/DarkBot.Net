using DarkBot.Net.Application.Extensions;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure;
using DarkBot.Net.Infrastructure.Logging;
using DarkBot.Net.Presentation.Ui.Config;
using DarkBot.Net.Presentation.Ui.Shell;
using DarkBot.Net.Presentation.ViewModels.Config;
using DarkBot.Net.Presentation.ViewModels.Login;
using DarkBot.Net.Presentation.ViewModels.Main;
using DarkBot.Net.Presentation.ViewModels.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Presentation.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationUi(this IServiceCollection services)
    {
        services.Configure<TestLoginOptions>(Program.Configuration.GetSection(TestLoginOptions.SectionName));
        services.Configure<GameApiOptions>(Program.Configuration.GetSection(GameApiOptions.SectionName));

        services.AddSingleton<IConfigWindowService, ConfigWindowService>();

        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ConfigTreeViewModel>();
        services.AddSingleton<StatsPanelViewModel>();
        services.AddSingleton<TitleBarDiagnosticsViewModel>();
        services.AddSingleton<ShellWindowViewModel>();

        services.AddSingleton<IShellWindowService, ShellWindowService>();

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
                services.AddInfrastructure();
                services.AddPresentationUi();
            })
            .Build();
}
