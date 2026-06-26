using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Core.Interfaces.Game;

public interface IGameSessionStore
{
    GameLaunchParameters? Current { get; }

    bool HasSession { get; }

    void Save(GameLaunchParameters parameters);

    void Clear();
}
