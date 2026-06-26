using DarkBot.Net.Application.Models.Bot;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasHeroDrawer
{
    public static void DrawTrail(MapCanvasRenderContext ctx)
    {
        if (ctx.IsLoading || ctx.HeroTrail.Count < 2)
            return;

        var gradient = MapCanvasPalette.TrailGradient;
        var totalSegments = Math.Max(ctx.HeroTrail.Count - 1, 1);

        using var trailPaint = new SKPaint
        {
            IsStroke = true,
            StrokeWidth = MapCanvasPalette.TrailStrokePx,
            StrokeCap = SKStrokeCap.Butt,
            IsAntialias = true
        };

        var hasPrevious = false;
        var previous = default(SKPoint);
        var segmentIndex = 0;

        foreach (var trailPoint in ctx.HeroTrail)
        {
            var screen = ctx.Transform.GameToScreen(trailPoint.X, trailPoint.Y);
            if (!hasPrevious)
            {
                previous = screen;
                hasPrevious = true;
                continue;
            }

            var gradientIndex = Math.Clamp(
                (int)((double)segmentIndex / totalSegments * (gradient.Length - 1)),
                0,
                gradient.Length - 1);
            trailPaint.Color = gradient[gradientIndex];
            ctx.Canvas.DrawLine(previous, screen, trailPaint);
            previous = screen;
            segmentIndex++;
        }
    }

    public static void DrawHeroAndPet(MapCanvasRenderContext ctx)
    {
        var hero = ctx.Map.Hero;
        if (!hero.OnMap)
            return;

        DrawPath(ctx);
        DrawViewBounds(ctx);

        var heroPoint = ctx.Transform.GameToScreen(hero.X, hero.Y);
        using (var heroPaint = new SKPaint
        {
            Color = MapCanvasPalette.Hero,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            ctx.Canvas.DrawCircle(heroPoint, MapCanvasPalette.HeroSizePx / 2f, heroPaint);
        }

        if (ctx.MoveTarget is { } clickTarget)
            DrawClickTarget(ctx, heroPoint, clickTarget);

        if (ctx.HasDisplayFlag(MapDisplayFlag.ShowPet) && ctx.Map.Pet is { Valid: true } pet)
            DrawPet(ctx, pet);
    }

    public static void DrawConfiguration(MapCanvasRenderContext ctx)
    {
        if (ctx.IsLoading)
            return;

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var paint = new SKPaint { Color = MapCanvasPalette.Text, IsAntialias = true };
        ctx.Canvas.DrawText(
            ctx.Map.Hero.Configuration,
            12,
            ctx.Height - 12,
            SKTextAlign.Left,
            font,
            paint);
    }

    private static void DrawPath(MapCanvasRenderContext ctx)
    {
        var hero = ctx.Map.Hero;
        using var line = new SKPaint
        {
            Color = MapCanvasPalette.Going,
            IsStroke = true,
            StrokeWidth = MapCanvasPalette.MoveLineStrokePx,
            IsAntialias = true
        };

        var begin = ctx.Transform.GameToScreen(hero.X, hero.Y);
        if (hero.PathSegments.Count > 0)
        {
            var current = begin;
            foreach (var segment in hero.PathSegments)
            {
                var next = ctx.Transform.GameToScreen(segment.X, segment.Y);
                ctx.Canvas.DrawLine(current, next, line);
                current = next;
            }
            return;
        }

        if (hero.Destination is { } dest)
        {
            var target = ctx.Transform.GameToScreen(dest.X, dest.Y);
            ctx.Canvas.DrawLine(begin, target, line);
        }
    }

    private static void DrawClickTarget(MapCanvasRenderContext ctx, SKPoint heroPoint, MapMoveTarget target)
    {
        var targetPoint = ctx.Transform.GameToScreen(target.GameX, target.GameY);
        using var line = new SKPaint
        {
            Color = MapCanvasPalette.Going,
            IsStroke = true,
            StrokeWidth = MapCanvasPalette.MoveLineStrokePx,
            IsAntialias = true
        };
        ctx.Canvas.DrawLine(heroPoint, targetPoint, line);
    }

    private static void DrawViewBounds(MapCanvasRenderContext ctx)
    {
        var bounds = ctx.Map.Hero.ViewBounds;
        if (bounds.Count < 3)
            return;

        using var path = new SKPath();
        var first = ctx.Transform.GameToScreen(bounds[0].X, bounds[0].Y);
        path.MoveTo(first);
        for (var i = 1; i < bounds.Count; i++)
        {
            var p = ctx.Transform.GameToScreen(bounds[i].X, bounds[i].Y);
            path.LineTo(p);
        }
        path.Close();

        using var stroke = new SKPaint
        {
            Color = MapCanvasPalette.BarrierBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        ctx.Canvas.DrawPath(path, stroke);
    }

    private static void DrawPet(MapCanvasRenderContext ctx, MapPetSnapshot pet)
    {
        var point = ctx.Transform.GameToScreen(pet.X, pet.Y);
        using var outer = new SKPaint { Color = MapCanvasPalette.Pet, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var inner = new SKPaint { Color = MapCanvasPalette.PetIn, Style = SKPaintStyle.Fill, IsAntialias = true };
        ctx.Canvas.DrawCircle(point, MapCanvasPalette.PetOuterSizePx / 2f, outer);
        ctx.Canvas.DrawCircle(point, MapCanvasPalette.PetInnerSizePx / 2f, inner);
    }
}
