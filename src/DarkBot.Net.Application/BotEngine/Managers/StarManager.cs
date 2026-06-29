using System.Collections.Concurrent;
using DarkBot.Net.Core.Entities;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Port of StarManager.byId — map names + static portal graph for UI.</summary>
public sealed class StarManager
{
    private static readonly GameMapModel Loading = new(-1, "Загрузка", "?");
    private readonly ConcurrentDictionary<int, GameMapModel> _maps = new();

    public StarManager() => _maps[-1] = Loading;

    public GameMapModel ById(int id)
    {
        if (id == -1)
            return Loading;

        if (StarMapRegistry.TryGet(id, out var name, out var shortName))
            return _maps.GetOrAdd(id, _ => new GameMapModel(id, name, shortName));

        if (id <= 0 || id >= MapLoadValidator.MaxMapId)
            return Loading;

        return _maps.GetOrAdd(id, static mapId => new GameMapModel(mapId, $"Map-{mapId}"));
    }

    public IReadOnlyList<MapPortalInfo> GetPortals(int mapId) => StarPortalRegistry.GetPortals(mapId);

    public IEnumerable<(int Id, string Name)> EnumerateKnownMaps()
    {
        foreach (var id in KnownMapIds)
            yield return (id, ById(id).Name);
    }

    private static readonly int[] KnownMapIds =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        21, 22, 23, 24, 25, 26, 27, 28, 29, 91, 92, 93, 94,
    ];
}
