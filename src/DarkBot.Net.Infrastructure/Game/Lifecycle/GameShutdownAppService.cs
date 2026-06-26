using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Infrastructure.Game.Lifecycle;

namespace DarkBot.Net.Infrastructure.Game.Lifecycle;

public sealed class GameShutdownAppService(GameShutdownCoordinator coordinator) : IGameShutdownAppService
{
    public Task StopGameClientAsync(CancellationToken cancellationToken = default) =>
        coordinator.StopGameClientAsync(cancellationToken);
}
