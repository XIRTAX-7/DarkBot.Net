using DarkBot.Net.Presentation.Services.Main.Map;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasDevDrawer
{
    public static void Draw(MapCanvasRenderContext ctx)
    {
        if (!ctx.HasDisplayFlag(MapDisplayFlag.DevStuff))
            return;

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontTinyPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        var lines = new[]
        {
            $"map {ctx.Map.MapId} {ctx.Map.MapWidth}x{ctx.Map.MapHeight}",
            $"entities npc={ctx.Map.Entities.Npcs.Count} box={ctx.Map.Entities.Boxes.Count}",
            $"hero ({ctx.Map.Hero.X:0},{ctx.Map.Hero.Y:0})"
        };

        var y = 40f;
        foreach (var line in lines)
        {
            ctx.Canvas.DrawText(line, 8, y, SKTextAlign.Left, font, paint);
            y += 12;
        }
    }
}
