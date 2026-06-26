using DarkBot.Net.Application.Models.Bot;
using DarkBot.Net.Presentation.Controls.Main.MapCanvas;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Tests;

public sealed class MapCanvasRendererTests
{
    [Fact]
    public void MapViewTransform_GameToScreen_UsesIndependentScale()
    {
        var transform = MapViewTransform.Create(new System.Windows.Size(800, 400), 21000, 13500);
        var point = transform.GameToScreen(10500, 6750);

        Assert.InRange(point.X, 395, 405);
        Assert.InRange(point.Y, 195, 205);
    }

    [Fact]
    public void MapViewTransform_ToScreenSizeW_ConvertsGameUnits()
    {
        var transform = MapViewTransform.Create(new System.Windows.Size(2100, 1350), 21000, 13500);
        var screenW = transform.ToScreenSizeW(3500);

        Assert.InRange(screenW, 349, 351);
    }

    [Fact]
    public void EntityDrawParams_FillAddsOne_RoundAddsTwo()
    {
        var fillEntity = new MapEntitySnapshot(1, 0, 0, MapEntityKind.Box, Fill: true);
        var (fillW, _, fillRound) = MapCanvasDrawHelpers.GetEntityDrawParams(fillEntity, roundEntities: true);

        Assert.Equal(5f, fillW);
        Assert.True(fillRound);

        var roundEntity = new MapEntitySnapshot(2, 0, 0, MapEntityKind.Npc);
        var (roundW, _, round) = MapCanvasDrawHelpers.GetEntityDrawParams(roundEntity, roundEntities: true);

        Assert.Equal(5f, roundW);
        Assert.True(round);
    }

    [Fact]
    public void TrailGradient_Has255StepsStartingAtAlphaOne()
    {
        var gradient = MapCanvasPalette.TrailGradient;

        Assert.Equal(255, gradient.Length);
        Assert.Equal(1, gradient[0].Alpha);
        Assert.Equal(255, gradient[^1].Alpha);
        Assert.Equal(MapCanvasPalette.TrailBase.Red, gradient[0].Red);
    }

    [Fact]
    public void Render_WithLoadingSnapshot_DoesNotThrow()
    {
        using var bitmap = new SKBitmap(320, 240);
        using var canvas = new SKCanvas(bitmap);
        var snapshot = new BotStatusSnapshot(
            false, 0, 0, 0, 0, 0, 0, 0,
            MapStatusSnapshot.Loading);

        MapCanvasRenderer.Render(canvas, 320, 240, snapshot, [], null, 0, 1);
    }
}
