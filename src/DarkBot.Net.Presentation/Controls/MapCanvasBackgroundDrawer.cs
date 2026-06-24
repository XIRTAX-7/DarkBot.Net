using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls;

internal static class MapCanvasBackgroundDrawer
{
    public static void Draw(MapCanvasRenderContext ctx)
    {
        var canvas = ctx.Canvas;
        var width = ctx.Width;
        var height = ctx.Height;
        var zoom = ctx.Map.Settings.MapZoom;

        if (zoom < 1.0 && !ctx.IsLoading)
        {
            canvas.Clear(MapCanvasPalette.Radiation);
            var mapW = (float)(width * zoom);
            var mapH = (float)(height * zoom);
            var left = (width - mapW) / 2f;
            var top = (height - mapH) / 2f;
            var mapRect = new SKRect(left, top, left + mapW, top + mapH);

            using var fill = new SKPaint { Color = MapCanvasPalette.Background, Style = SKPaintStyle.Fill };
            using var border = new SKPaint
            {
                Color = MapCanvasPalette.MapZoomBorder,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };
            canvas.DrawRect(mapRect, fill);
            canvas.DrawRect(mapRect, border);
            return;
        }

        canvas.Clear(MapCanvasPalette.Background);
    }
}
