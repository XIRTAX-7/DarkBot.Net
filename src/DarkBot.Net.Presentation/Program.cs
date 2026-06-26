using System.Globalization;
using System.Windows;
using DarkBot.Net.Presentation.Controls.Config;
using DarkBot.Net.Presentation.Controls.Main;
using DarkBot.Net.Presentation.Extensions;
using DarkBot.Net.Infrastructure.Logging;
using DarkBot.Net.Presentation.Diagnostics;
using DarkBot.Net.Presentation.ViewModels.Config;
using DarkBot.Net.Presentation.ViewModels.Login;
using DarkBot.Net.Presentation.ViewModels.Main;
using DarkBot.Net.Presentation.Views.Login;
using DarkBot.Net.Presentation.Views.Main;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using ReactiveUI.Builder;
using Serilog;
using Splat;

namespace DarkBot.Net.Presentation;

internal static class Program
{
    public static IHost AppHost { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var russianCulture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = russianCulture;
        CultureInfo.DefaultThreadCurrentUICulture = russianCulture;
        Thread.CurrentThread.CurrentCulture = russianCulture;
        Thread.CurrentThread.CurrentUICulture = russianCulture;

        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "DARKBOT_")
            .Build();

        DarkBotSerilogHostBuilderExtensions.ConfigureBootstrapLogger(Configuration);

        try
        {
            Log.Information(
                "DarkBot.Net UI starting (base directory: {BaseDirectory}, BrowserApi: {BrowserApi}, logs: {LogDirectory})",
                AppContext.BaseDirectory,
                Configuration.GetValue<string>("DarkBot:BrowserApi") ?? "default",
                System.IO.Path.Combine(AppContext.BaseDirectory, "logs"));

            ConfigureReactiveUi();

            AppHost = DependencyInjection.BuildDarkBotHost(args, Configuration);
            try
            {
                AppHost.Start();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start application host (DI or hosted service error)");
                Log.CloseAndFlush();
                throw;
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "DarkBot.Net UI terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("DarkBot.Net UI shutting down");
            try
            {
                if (AppHost is not null)
                {
                    using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                    try
                    {
                        AppHost.StopAsync(stopTimeout.Token).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Warning(
                            "Application host stop timed out after 12s — some hosted services may still be running");
                    }
                }

                AppHost?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error stopping application host");
            }

            Log.CloseAndFlush();
        }
    }

    private static void ConfigureReactiveUi()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithWpf()
            .BuildApp();

        Locator.CurrentMutable.RegisterViewForViewModel<LoginView, LoginViewModel>();
        Locator.CurrentMutable.RegisterViewForViewModel<MainView, MainWindowViewModel>();
        Locator.CurrentMutable.RegisterViewForViewModel<ConfigTreeControl, ConfigTreeViewModel>();
        Locator.CurrentMutable.RegisterViewForViewModel<StatsPanelControl, StatsPanelViewModel>();
    }
}
