using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Core.Bot;

/// <summary>Port of Main bot loop — target 10 Hz (100 ms).</summary>
public sealed class BotLoopService : BackgroundService, IBotController
{
    public const int TargetTickMs = 100;

    private readonly BotRuntime _runtime;
    private readonly BotInstallerService _installer;
    private readonly StatsManager _stats;
    private readonly ILogger<BotLoopService> _logger;
    private volatile bool _running;
    private long _tickCount;
    private double _lastTickMs;

    public BotLoopService(
        BotRuntime runtime,
        BotInstallerService installer,
        StatsManager stats,
        ILogger<BotLoopService> logger)
    {
        _runtime = runtime;
        _installer = installer;
        _stats = stats;
        _logger = logger;
    }

    public bool IsRunning => _running;
    public long TickCount => Interlocked.Read(ref _tickCount);
    public double LastTickMs => _lastTickMs;

    public void Start() => _running = true;

    public void Pause() => _running = false;

    public void Stop() => _running = false;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot loop started ({TargetHz} Hz target)", 1000 / TargetTickMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            var started = Environment.TickCount64;

            try
            {
                _installer.Tick();
                _runtime.Tick(_running);
                Interlocked.Increment(ref _tickCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bot tick failed");
            }
            finally
            {
                var elapsed = Environment.TickCount64 - started;
                _lastTickMs = elapsed;
                _stats.TickAverageStats(elapsed);
            }

            var targetDelay = _installer.GetRecommendedDelayMs();
            var delay = Math.Max(0, targetDelay - (int)_lastTickMs);
            if (delay > 0)
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
    }
}
