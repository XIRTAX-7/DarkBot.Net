using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DarkBot.Net.Presentation.Logging;

internal static class DarkBotSerilogHostExtensions
{
    public static IHostBuilder UseDarkBotSerilog(this IHostBuilder hostBuilder, IConfiguration configuration) =>
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "DarkBot.Net");
        });

    public static void ConfigureBootstrapLogger(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Application", "DarkBot.Net")
            .CreateBootstrapLogger();
    }
}
