using DarkBot.Net.Api.Game;

namespace DarkBot.Net.Api.Managers;

/// <summary>Port of eu.darkbot.api.managers.StarSystemAPI (subset).</summary>
public interface IStarSystemApi : IApi.ISingleton
{
    IGameMap GetByName(string mapName);
    IGameMap GetById(int mapId);
}
