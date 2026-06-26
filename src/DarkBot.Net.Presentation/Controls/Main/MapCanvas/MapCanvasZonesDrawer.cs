using DarkBot.Net.Presentation.Services;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasZonesDrawer
{
    private const int ZoneColumns = 30;
    private const int ZoneRows = 30;

    public static void Draw(MapCanvasRenderContext ctx)
    {
        DrawAgentZones(ctx);
        if (ctx.HasDisplayFlag(MapDisplayFlag.Zones))
            DrawConfigGridZones(ctx);
    }

    private static void DrawAgentZones(MapCanvasRenderContext ctx)
    {
        foreach (var zone in ctx.Map.Zones.Barriers)
            DrawPolygonZone(ctx, zone, MapCanvasPalette.Barrier, MapCanvasPalette.BarrierBorder);

        foreach (var zone in ctx.Map.Zones.Mists)
            DrawPolygonZone(ctx, zone, MapCanvasPalette.NoCloak, null);

        foreach (var safety in ctx.Map.Zones.SafetyCircles)
            DrawSafety(ctx, safety);
    }

    private static void DrawConfigGridZones(MapCanvasRenderContext ctx)
    {
        DrawGrid(ctx, ctx.Map.Zones.PreferGrid, MapCanvasPalette.Prefer);
        DrawGrid(ctx, ctx.Map.Zones.AvoidGrid, MapCanvasPalette.Avoid);
    }

    private static void DrawPolygonZone(
        MapCanvasRenderContext ctx,
        MapPolygonZoneSnapshot zone,
        SKColor fill,
        SKColor? stroke)
    {
        if (zone.Polygon.Count < 3)
            return;

        using var path = new SKPath();
        var first = ctx.Transform.GameToScreen(zone.Polygon[0].X, zone.Polygon[0].Y);
        path.MoveTo(first);
        for (var i = 1; i < zone.Polygon.Count; i++)
        {
            var p = ctx.Transform.GameToScreen(zone.Polygon[i].X, zone.Polygon[i].Y);
            path.LineTo(p);
        }
        path.Close();

        using var fillPaint = new SKPaint { Color = fill, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvasDrawPath(ctx.Canvas, path, fillPaint);

        if (stroke is { } strokeColor)
        {
            using var strokePaint = new SKPaint
            {
                Color = strokeColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };
            canvasDrawPath(ctx.Canvas, path, strokePaint);
        }
    }

    private static void canvasDrawPath(SKCanvas canvas, SKPath path, SKPaint paint) =>
        canvas.DrawPath(path, paint);

    private static void DrawSafety(MapCanvasRenderContext ctx, MapSafetyZoneSnapshot safety)
    {
        var center = ctx.Transform.GameToScreen(safety.X, safety.Y);
        var rx = ctx.Transform.ToScreenSizeW(safety.DiameterGame) / 2f;
        var ry = ctx.Transform.ToScreenSizeH(safety.DiameterGame) / 2f;
        using var paint = new SKPaint { Color = MapCanvasPalette.Safety, Style = SKPaintStyle.Fill, IsAntialias = true };
        ctx.Canvas.DrawOval(center.X - rx, center.Y - ry, center.X + rx, center.Y + ry, paint);
    }

    private static void DrawGrid(
        MapCanvasRenderContext ctx,
        IReadOnlyList<MapZoneCell> zones,
        SKColor? overrideFill)
    {
        if (zones.Count == 0)
            return;

        var mapRect = ctx.Transform.MapRect;
        var zoneWidth = mapRect.Width / ZoneColumns;
        var zoneHeight = mapRect.Height / ZoneRows;

        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var strokePaint = new SKPaint { IsStroke = true, StrokeWidth = 1f, IsAntialias = true };

        foreach (var zone in zones)
        {
            if (zone.Column is < 0 or >= ZoneColumns || zone.Row is < 0 or >= ZoneRows)
                continue;

            var left = mapRect.Left + zone.Column * zoneWidth;
            var top = mapRect.Top + zone.Row * zoneHeight;
            var zoneRect = new SKRect(left, top, left + zoneWidth, top + zoneHeight);

            fillPaint.Color = overrideFill ?? GetZoneFillColor(zone.Kind);
            strokePaint.Color = GetZoneStrokeColor(zone.Kind);
            ctx.Canvas.DrawRect(zoneRect, fillPaint);
            ctx.Canvas.DrawRect(zoneRect, strokePaint);
        }
    }

    private static SKColor GetZoneFillColor(MapZoneKind kind) =>
        kind switch
        {
            MapZoneKind.Preferred => MapCanvasPalette.Prefer,
            MapZoneKind.Forbidden => MapCanvasPalette.Avoid,
            MapZoneKind.Safe => MapCanvasPalette.Safety,
            _ => MapCanvasPalette.Unknown.WithAlpha(70)
        };

    private static SKColor GetZoneStrokeColor(MapZoneKind kind) =>
        kind switch
        {
            MapZoneKind.Preferred => MapCanvasPalette.Prefer.WithAlpha(96),
            MapZoneKind.Forbidden => MapCanvasPalette.Avoid.WithAlpha(96),
            MapZoneKind.Safe => MapCanvasPalette.Safety.WithAlpha(160),
            _ => MapCanvasPalette.Unknown.WithAlpha(150)
        };
}
