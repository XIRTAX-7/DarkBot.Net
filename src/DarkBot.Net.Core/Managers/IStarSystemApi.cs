using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Core.Managers;

/// <summary>Port of eu.darkbot.api.managers.StarSystemAPI (subset).</summary>
public interface IStarSystemApi : IApi.ISingleton
{
    IGameMap GetByName(string mapName);
    IGameMap GetById(int mapId);
}
