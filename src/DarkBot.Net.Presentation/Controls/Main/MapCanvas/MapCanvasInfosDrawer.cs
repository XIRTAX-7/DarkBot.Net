using DarkBot.Net.Application.DTOs.Responses.Bot;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasInfosDrawer
{
    public static void Draw(MapCanvasRenderContext ctx)
    {
        if (ctx.IsLoading)
        {
            DrawLoading(ctx);
            return;
        }

        DrawMapName(ctx);
        DrawStatus(ctx);
        DrawModuleLines(ctx);
        DrawSid(ctx);
        DrawHealthBlocks(ctx);
    }

    private static void DrawLoading(MapCanvasRenderContext ctx)
    {
        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontBigPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        ctx.Canvas.DrawText(
            ctx.Map.MapName,
            ctx.Width / 2f,
            ctx.Height / 2f,
            SKTextAlign.Center,
            font,
            paint);
    }

    private static void DrawMapName(MapCanvasRenderContext ctx)
    {
        var name = ctx.Map.Overlay.MapName;
        if (!string.IsNullOrEmpty(ctx.Map.Overlay.NextMapName))
            name += "→" + ctx.Map.Overlay.NextMapName;

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontBigPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        ctx.Canvas.DrawText(name, ctx.Width / 2f, ctx.Height / 2f - 5, SKTextAlign.Center, font, paint);
    }

    private static void DrawStatus(MapCanvasRenderContext ctx)
    {
        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        ctx.Canvas.DrawText(
            ctx.Map.Overlay.StatusLine,
            ctx.Width / 2f,
            ctx.Height / 2f + 35,
            SKTextAlign.Center,
            font,
            paint);
    }

    private static void DrawModuleLines(MapCanvasRenderContext ctx)
    {
        if (ctx.Map.Overlay.ModuleStatusLines.Count == 0)
            return;

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        var y = 12f;
        foreach (var line in ctx.Map.Overlay.ModuleStatusLines)
        {
            y += 14;
            ctx.Canvas.DrawText(line, 7, y, SKTextAlign.Left, font, paint);
        }
    }

    private static void DrawSid(MapCanvasRenderContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.Map.Overlay.Sid))
            return;

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        ctx.Canvas.DrawText(
            "SID: " + ctx.Map.Overlay.Sid,
            ctx.Width - 5,
            12,
            SKTextAlign.Right,
            font,
            paint);
    }

    private static void DrawHealthBlocks(MapCanvasRenderContext ctx)
    {
        var hero = ctx.Map.Hero;
        if (!hero.Valid)
            return;

        const int marginX = 10;
        const int bottomOffset = 34;
        var barWidth = ctx.Width / 2f - 20;
        if (barWidth <= 0)
            return;

        var top = ctx.Height - bottomOffset;
        MapCanvasDrawHelpers.DrawHealthBar(
            ctx.Canvas,
            marginX,
            top,
            barWidth,
            MapCanvasPalette.HealthBarHeightPx,
            hero.Hp,
            hero.MaxHp,
            hero.Nano,
            hero.MaxNano,
            hero.Shield,
            hero.MaxShield);

        if (ctx.HasDisplayFlag(MapDisplayFlag.HeroName) && !string.IsNullOrEmpty(hero.Name))
        {
            using var font = new SKFont(MapCanvasPalette.SansTypeface, MapCanvasPalette.FontSmallPx);
            using var paint = new SKPaint { Color = MapCanvasPalette.Text, IsAntialias = true };
            ctx.Canvas.DrawText(hero.Name, marginX, top - 4, SKTextAlign.Left, font, paint);
        }

        if (ctx.HasDisplayFlag(MapDisplayFlag.HpShieldNum))
            DrawHealthNumbers(ctx, marginX, top, barWidth, hero);

        DrawPetBlock(ctx);
        DrawTargetBlock(ctx);
    }

    private static void DrawHealthNumbers(
        MapCanvasRenderContext ctx,
        float left,
        float top,
        float width,
        MapHeroSnapshot hero)
    {
        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontTinyPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.Text, IsAntialias = true };
        var text = $"{MapCanvasDrawHelpers.FormatHealthNumber(hero.Hp)}/{MapCanvasDrawHelpers.FormatHealthNumber(hero.MaxHp)}";
        ctx.Canvas.DrawText(text, left + width / 2f, top + 10, SKTextAlign.Center, font, paint);
    }

    private static void DrawPetBlock(MapCanvasRenderContext ctx)
    {
        if (ctx.Map.Pet is not { Valid: true } pet)
            return;

        var left = ctx.Width / 2f + 10;
        var top = ctx.Height - 34;
        var width = ctx.Width / 2f - 20;
        MapCanvasDrawHelpers.DrawHealthBar(ctx.Canvas, left, top, width, 8, pet.Hp, pet.MaxHp);

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontTinyPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.Fuel, IsAntialias = true };
        ctx.Canvas.DrawText($"fuel {pet.Fuel}/{pet.MaxFuel}", left, top - 4, SKTextAlign.Left, font, paint);

        if (!string.IsNullOrEmpty(pet.TargetName))
        {
            ctx.Canvas.DrawText(pet.TargetName, left + width, top - 4, SKTextAlign.Right, font, paint);
            MapCanvasDrawHelpers.DrawHealthBar(
                ctx.Canvas, left, top + 10, width, 4, pet.TargetHp, pet.TargetMaxHp);
        }
    }

    private static void DrawTargetBlock(MapCanvasRenderContext ctx)
    {
        var target = ctx.Map.Target;
        if (target is not { Id: > 0 })
            return;

        var right = ctx.Width - 10f;
        var top = ctx.Height / 2f;
        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };

        if (!string.IsNullOrEmpty(target.Name))
            ctx.Canvas.DrawText(target.Name, right, top, SKTextAlign.Right, font, paint);

        var barTop = top + 6;
        MapCanvasDrawHelpers.DrawHealthBar(
            ctx.Canvas,
            right - 120,
            barTop,
            120,
            8,
            target.Hp,
            target.MaxHp);
    }
}
