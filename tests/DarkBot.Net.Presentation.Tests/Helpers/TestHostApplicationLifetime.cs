using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Presentation.Tests.Helpers;

internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _stopping = new();

    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => _stopping.Token;

    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void SignalStopping() => _stopping.Cancel();

    public void StopApplication() => SignalStopping();
}
