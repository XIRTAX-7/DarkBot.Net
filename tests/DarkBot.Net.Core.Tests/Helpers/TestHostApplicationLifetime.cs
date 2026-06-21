using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Application.Tests.Helpers;

internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly CancellationTokenSource _stopped = new();

    public CancellationToken ApplicationStarted => _started.Token;

    public CancellationToken ApplicationStopping => _stopping.Token;

    public CancellationToken ApplicationStopped => _stopped.Token;

    public void SignalStarted() => _started.Cancel();

    public void SignalStopping() => _stopping.Cancel();

    public void SignalStopped() => _stopped.Cancel();

    public void StopApplication() => SignalStopping();
}
