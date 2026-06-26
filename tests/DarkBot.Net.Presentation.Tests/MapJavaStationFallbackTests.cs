using DarkBot.Net.Application.Mappers.Bot;
using DarkBot.Net.Application.DTOs.Responses.Bot;

namespace DarkBot.Net.Presentation.Tests;

public sealed class MapJavaStationFallbackTests
{
    [Fact]
    public void MergeStations_On1_1WithoutLiveData_UsesCenterFallback()
    {
        var stations = MapJavaStationFallback.MergeStations([], "1-1", 21000, 13500);

        Assert.Equal(6, stations.Count);
        Assert.Contains(stations, s => s.SubKind == "hq" && s.X == 10500 && s.Y == 6750);
    }

    [Fact]
    public void MergeStations_On2_1WithoutLiveData_ReturnsEmpty()
    {
        var stations = MapJavaStationFallback.MergeStations([], "2-1", 21000, 13500);

        Assert.Empty(stations);
    }

    [Fact]
    public void MergeStations_On2_1_KeepsLiveEicBasePositions()
    {
        var live = new[]
        {
            new MapEntitySnapshot(150000043, 19000, 2000, MapEntityKind.BaseSpot, SubKind: "hq"),
            new MapEntitySnapshot(150000044, 19000, 3080, MapEntityKind.BaseSpot, SubKind: "spot"),
            new MapEntitySnapshot(150000045, 20080, 2000, MapEntityKind.BaseSpot, SubKind: "spot"),
            new MapEntitySnapshot(150000032, 18843, 206, MapEntityKind.StationTurret, SubKind: "turret"),
        };

        var stations = MapJavaStationFallback.MergeStations(live, "2-1", 21000, 13500);

        Assert.Contains(stations, s => s.SubKind == "hq" && s.X == 19000 && s.Y == 2000);
        Assert.Contains(stations, s => s.Kind == MapEntityKind.StationTurret);
        Assert.DoesNotContain(stations, s => s.X == 10500 && s.Y == 6750);
    }

    [Fact]
    public void MergeStations_FiltersOutOfBoundsLiveData_On2_1_ReturnsEmpty()
    {
        var live = new[]
        {
            new MapEntitySnapshot(2, 1000, -500, MapEntityKind.BaseSpot, SubKind: "hq")
        };

        var stations = MapJavaStationFallback.MergeStations(live, "2-1", 21000, 13500);

        Assert.Empty(stations);
    }

    [Fact]
    public void MergeStations_ClustersAroundHq_NotMapCenter()
    {
        var live = new[]
        {
            new MapEntitySnapshot(2, 19000, 2000, MapEntityKind.BaseSpot, SubKind: "hq"),
            new MapEntitySnapshot(3, 19000, 3080, MapEntityKind.BaseSpot, SubKind: "spot"),
            new MapEntitySnapshot(99, 10500, 300, MapEntityKind.BaseSpot, SubKind: "hq"),
        };

        var stations = MapJavaStationFallback.MergeStations(live, "2-1", 21000, 13500);

        Assert.Contains(stations, s => s.Id == 2);
        Assert.Contains(stations, s => s.Id == 3);
        Assert.DoesNotContain(stations, s => s.Id == 99);
    }
}
