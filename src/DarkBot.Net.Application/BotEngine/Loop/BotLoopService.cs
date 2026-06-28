using System.Diagnostics;
using DarkBot.Net.Application.BotEngine.Install;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.BotEngine.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.BotEngine.Loop;

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
    private double _lastLoopPeriodMs;

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

    /// <summary>Фактический период итерации loop (работа tick + delay), мс.</summary>
    public double LastLoopPeriodMs => _lastLoopPeriodMs;

    public void Start() => _running = true;

    public void Pause() => _running = false;

    /// <summary>
    /// Останавливает tick-логику бота. Безопасно вызывать из UI-потока:
    /// <see cref="_running"/> — volatile, tick читает его атомарно между итерациями.
    /// </summary>
    public void Stop() => _running = false;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot loop started ({TargetHz} Hz target)", 1000 / TargetTickMs);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var started = Stopwatch.GetTimestamp();

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
                    var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    _lastTickMs = elapsedMs;
                    _stats.TickAverageStats(elapsedMs);
                }

                var targetDelay = _installer.GetRecommendedDelayMs();
                var delay = Math.Max(0, targetDelay - (int)Math.Ceiling(_lastTickMs));
                _lastLoopPeriodMs = _lastTickMs + delay;

                if (delay > 0)
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when the host cancels the background service during shutdown.
        }

        _logger.LogInformation("Bot loop stopped");
    }
}
