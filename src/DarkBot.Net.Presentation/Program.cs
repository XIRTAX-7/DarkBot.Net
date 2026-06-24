using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.Logging;
using DarkBot.Net.Presentation.ViewModels;
using DarkBot.Net.Presentation.Views.Login;
using DarkBot.Net.Presentation.Views.Main;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using ReactiveUI.Builder;
using Serilog;
using Splat;
using System.Windows;

namespace DarkBot.Net.Presentation;

internal static class Program
{
    public static IHost AppHost { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "DARKBOT_")
            .Build();

        DarkBotSerilogHostExtensions.ConfigureBootstrapLogger(Configuration);

        try
        {
            Log.Information(
                "DarkBot.Net UI starting (base directory: {BaseDirectory}, BrowserApi: {BrowserApi}, logs: {LogDirectory})",
                AppContext.BaseDirectory,
                Configuration.GetValue<string>("DarkBot:BrowserApi") ?? "default",
                System.IO.Path.Combine(AppContext.BaseDirectory, "logs"));

            ConfigureReactiveUi();

            AppHost = ServiceCollectionExtensions.BuildDarkBotHost(args, Configuration);
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
