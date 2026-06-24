using Avalonia;
using DarkBot.Net.Presentation.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ReactiveUI.Avalonia;
using Serilog;

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
                Path.Combine(AppContext.BaseDirectory, "logs"));

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

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .UseReactiveUI(_ => { })
            .RegisterReactiveUIViewsFromEntryAssembly()
            .LogToTrace();
}
