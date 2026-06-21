using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;

namespace DarkBot.Net.Core.Interfaces.Game;

public interface IGameLauncherService
{
    IGameConnection ActiveConnection { get; }

    Task<GameClientConnectResult> LaunchAndConnectAsync(
        GameLaunchParameters launch,
        CancellationToken cancellationToken = default);

    Task LaunchAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default);

    Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default);

    Task<GameClientConnectResult> RestartClientAsync(
        GameLaunchParameters launch,
        CancellationToken cancellationToken = default);

    void AttachProcess(long pid);
}
