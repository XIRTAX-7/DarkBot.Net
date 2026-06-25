using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Presentation.Services;

/// <summary>
/// Запасные станции — только когда live-данные из игры пусты.
/// Как Java: координаты только из игры; fallback — только для 1-1 (MMO база в центре).
/// </summary>
internal static class MapJavaStationFallback
{
    /// <summary>Радиус кластера вокруг HQ (spots/turrets рядом с штабом).</summary>
    private const int HomeBaseClusterRadius = 6000;

    private const int HomeSpotOffset = 1800;

    public static IReadOnlyList<MapEntitySnapshot> MergeStations(
        IReadOnlyList<MapEntitySnapshot> live,
        string? mapName,
        int mapWidth,
        int mapHeight)
    {
        var inBounds = live
            .Where(s => MapCoordinateBounds.IsInSafeBounds(s.X, s.Y, mapWidth, mapHeight))
            .ToArray();

        if (inBounds.Length > 0)
        {
            return ClusterAroundHeadquarter(inBounds);
        }

        if (mapName == "1-1" && mapWidth > 0 && mapHeight > 0)
        {
            return CreateHomeBaseFallback(mapWidth, mapHeight);
        }

        return [];
    }

    /// <summary>
    /// База на 2-1/3-1 не в геометрическом центре карты (EIC: ~19000,2000).
    /// Кластеризуем вокруг HQ, если он есть.
    /// </summary>
    private static IReadOnlyList<MapEntitySnapshot> ClusterAroundHeadquarter(
        IReadOnlyList<MapEntitySnapshot> inBounds)
    {
        var hq = inBounds.FirstOrDefault(s => s.SubKind == "hq");
        if (hq is null)
        {
            return inBounds;
        }

        var radiusSq = (double)HomeBaseClusterRadius * HomeBaseClusterRadius;
        var clustered = inBounds
            .Where(s =>
            {
                var dx = s.X - hq.X;
                var dy = s.Y - hq.Y;
                return dx * dx + dy * dy <= radiusSq;
            })
            .ToArray();

        return clustered.Length > 0 ? clustered : inBounds;
    }

    private static IReadOnlyList<MapEntitySnapshot> CreateHomeBaseFallback(int mapWidth, int mapHeight)
    {
        var cx = mapWidth / 2.0;
        var cy = mapHeight / 2.0;
        var offset = HomeSpotOffset;

        return
        [
            new MapEntitySnapshot(1, cx, cy, MapEntityKind.BaseSpot, SubKind: "hq", Label: "headquarters"),
            new MapEntitySnapshot(2, cx, cy, MapEntityKind.BaseSpot, SubKind: "home", Label: "station"),
            new MapEntitySnapshot(3, cx - offset, cy - offset, MapEntityKind.BaseSpot, SubKind: "spot", Label: "hangar"),
            new MapEntitySnapshot(4, cx + offset, cy - offset, MapEntityKind.BaseSpot, SubKind: "spot", Label: "refinery"),
            new MapEntitySnapshot(5, cx - offset, cy + offset, MapEntityKind.BaseSpot, SubKind: "spot", Label: "repairstation"),
            new MapEntitySnapshot(6, cx + offset, cy + offset, MapEntityKind.BaseSpot, SubKind: "spot", Label: "questgiver"),
        ];
    }
}
