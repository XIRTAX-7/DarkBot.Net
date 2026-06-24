using System.Text.RegularExpressions;
using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Presentation.Services;

/// <summary>
/// Запасные станции для отрисовки овалов — только когда live-данные из игры пусты.
/// Диаметры задаёт MapCanvasEntitiesDrawer (как ConstantEntitiesDrawer.java).
/// Позиция HQ на home-карте: центр карты (как в игре для x-1).
/// </summary>
internal static partial class MapJavaStationFallback
{
    private static readonly Regex HomeMapNamePattern = HomeMapRegex();

    public static IReadOnlyList<MapEntitySnapshot> MergeStations(
        IReadOnlyList<MapEntitySnapshot> live,
        string? mapName,
        int mapWidth,
        int mapHeight)
    {
        var valid = live
            .Where(s => MapCoordinateBounds.IsInSafeBounds(s.X, s.Y, mapWidth, mapHeight))
            .ToArray();

        if (valid.Length > 0)
        {
            return valid;
        }

        if (mapWidth <= 0 || mapHeight <= 0 || !IsHomeMap(mapName))
        {
            return [];
        }

        return
        [
            new MapEntitySnapshot(
                Id: 1,
                X: mapWidth / 2.0,
                Y: mapHeight / 2.0,
                Kind: MapEntityKind.BaseSpot,
                SubKind: "hq",
                Label: "headquarters")
        ];
    }

    private static bool IsHomeMap(string? mapName) =>
        mapName is not null && HomeMapNamePattern.IsMatch(mapName);

    [GeneratedRegex(@"^[1-8]-1$")]
    private static partial Regex HomeMapRegex();
}
