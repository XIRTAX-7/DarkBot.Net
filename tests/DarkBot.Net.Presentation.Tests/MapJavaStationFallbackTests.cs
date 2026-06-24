using DarkBot.Net.Presentation.Services;

namespace DarkBot.Net.Presentation.Tests;

public sealed class MapJavaStationFallbackTests
{
    [Fact]
    public void MergeStations_OnHomeMapWithoutLiveData_AddsHeadquarterAtCenter()
    {
        var stations = MapJavaStationFallback.MergeStations([], "1-1", 21000, 13500);

        Assert.Single(stations);
        Assert.Equal(10500, stations[0].X);
        Assert.Equal(6750, stations[0].Y);
        Assert.Equal("hq", stations[0].SubKind);
    }

    [Fact]
    public void MergeStations_FiltersOutOfBoundsLiveData_UsesHomeFallback()
    {
        var live = new[]
        {
            new MapEntitySnapshot(2, 1000, -500, MapEntityKind.BaseSpot, SubKind: "hq")
        };

        var stations = MapJavaStationFallback.MergeStations(live, "2-1", 21000, 13500);

        Assert.Single(stations);
        Assert.Equal(10500, stations[0].X);
        Assert.Equal("hq", stations[0].SubKind);
    }
}
