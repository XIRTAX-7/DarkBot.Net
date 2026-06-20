using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>
/// Runs all embedded JVM / KekkaPlayer work on a dedicated STA thread.
/// Java DarkBot never initializes AWT/COM on the same thread as a foreign UI toolkit.
/// </summary>
public sealed class KekkaJvmHost : IHostedService, IDisposable
{
    private readonly ILogger<KekkaJvmHost> _logger;
    private Thread? _thread;
    private BlockingCollection<(Action Action, TaskCompletionSource<object?> Tcs)>? _queue;
    private CancellationTokenSource? _cts;

    public KekkaJvmHost(ILogger<KekkaJvmHost> logger) => _logger = logger;

    public int ThreadId => _thread?.ManagedThreadId ?? -1;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _queue = new BlockingCollection<(Action, TaskCompletionSource<object?>)>();
        _thread = new Thread(RunLoop)
        {
            Name = "KekkaPlayer-JVM-Host",
            IsBackground = true,
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _logger.LogInformation("KekkaPlayer JVM host thread started (STA, threadId pending)");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _queue?.CompleteAdding();
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _queue?.CompleteAdding();
        _cts?.Cancel();
        _cts?.Dispose();
        _queue?.Dispose();
    }

    public Task RunAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (_queue is null || _thread is null)
            throw new InvalidOperationException("KekkaPlayer JVM host is not running.");

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        _queue.Add((() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, tcs));

        return tcs.Task;
    }

    public Task RunAsync(Action action, CancellationToken cancellationToken = default) =>
        RunAsync(() =>
        {
            action();
            return Task.CompletedTask;
        }, cancellationToken);

    public bool IsJvmThread => _thread is not null && Thread.CurrentThread == _thread;

    private void RunLoop()
    {
        _logger.LogInformation("KekkaPlayer JVM host thread ready (threadId={ThreadId})", Environment.CurrentManagedThreadId);

        try
        {
            foreach (var (action, tcs) in _queue!.GetConsumingEnumerable(_cts!.Token))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    if (!tcs.Task.IsCompleted)
                        tcs.TrySetException(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KekkaPlayer JVM host thread failed");
        }
    }
}
