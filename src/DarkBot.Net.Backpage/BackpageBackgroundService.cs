using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Backpage;

/// <summary>Port of BackpageManager daemon thread — SID validation loop.</summary>
public sealed class BackpageBackgroundService : BackgroundService
{
    private readonly BackpageService _backpage;
    private readonly ILogger<BackpageBackgroundService> _logger;

    public BackpageBackgroundService(BackpageService backpage, ILogger<BackpageBackgroundService> logger)
    {
        _backpage = backpage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backpage background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _backpage.CheckSidValid();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backpage SID check failed");
            }

            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
        }
    }
}
