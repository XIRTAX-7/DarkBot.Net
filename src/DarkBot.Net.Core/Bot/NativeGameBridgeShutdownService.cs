using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Hosting;

namespace DarkBot.Net.Core.Bot;

public sealed class NativeGameBridgeShutdownService : IHostedService
{
    private readonly NativeGameBridge _bridge;

    public NativeGameBridgeShutdownService(NativeGameBridge bridge) => _bridge = bridge;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _bridge.Dispose();
        return Task.CompletedTask;
    }
}
