using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Core.Tests;

public sealed class MapCoordinateBoundsTests
{
    [Theory]
    [InlineData(100, 200, 21000, 13500, true)]
    [InlineData(0, 0, 21000, 13500, true)]
    [InlineData(21000, 13500, 21000, 13500, true)]
    [InlineData(-1, 100, 21000, 13500, false)]
    [InlineData(100, 14000, 21000, 13500, false)]
    public void IsInBounds_respects_map_edges(
        double x,
        double y,
        int width,
        int height,
        bool expected)
    {
        Assert.Equal(expected, MapCoordinateBounds.IsInBounds(x, y, width, height));
    }

    [Fact]
    public void Clamp_keeps_coordinates_inside_map()
    {
        var (x, y) = MapCoordinateBounds.Clamp(-500, 25000, 21000, 13500);

        Assert.Equal(0, x);
        Assert.Equal(13500, y);
    }

    [Fact]
    public void SafeClamp_keeps_coordinates_inside_safe_margin()
    {
        var (x, y) = MapCoordinateBounds.SafeClamp(-500, 25000, 21000, 13500);

        Assert.Equal(MapCoordinateBounds.SafeMargin, x);
        Assert.Equal(13500 - MapCoordinateBounds.SafeMargin, y);
    }
}
