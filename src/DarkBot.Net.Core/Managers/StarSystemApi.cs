using DarkBot.Net.Api.Game;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Entities;

namespace DarkBot.Net.Core.Managers;

public sealed class StarSystemApi : IStarSystemApi
{
    private readonly StarManager _stars;
    private readonly Dictionary<string, int> _nameToId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["5-2"] = 52,
        ["5-3"] = 53
    };

    public StarSystemApi(StarManager stars) => _stars = stars;

    public IGameMap GetByName(string mapName)
    {
        if (_nameToId.TryGetValue(mapName, out var id))
            return _stars.ById(id);

        return new GameMapModel(-1, mapName, mapName);
    }

    public IGameMap GetById(int mapId) => _stars.ById(mapId);
}
