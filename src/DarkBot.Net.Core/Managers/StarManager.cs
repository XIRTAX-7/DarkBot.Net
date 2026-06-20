using System.Collections.Concurrent;
using DarkBot.Net.Core.Entities;

namespace DarkBot.Net.Core.Managers;

/// <summary>Minimal port of StarManager.byId — full graph in Phase 3+.</summary>
public sealed class StarManager
{
    private static readonly GameMapModel Loading = new(-1, "Loading", "?");
    private readonly ConcurrentDictionary<int, GameMapModel> _maps = new();

    public StarManager() => _maps[-1] = Loading;

    public GameMapModel ById(int id)
    {
        if (id == -1)
            return Loading;

        return _maps.GetOrAdd(id, static mapId => new GameMapModel(mapId, $"Map-{mapId}"));
    }
}
