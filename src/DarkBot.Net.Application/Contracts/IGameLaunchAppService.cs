using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Application.Contracts;

public interface IGameLaunchAppService
{
    void ScheduleLaunch(GameLaunchParameters launch);

    Task LaunchAndConnectAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default);
}
