using DarkBot.Net.Presentation.Services;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls;

internal static class MapCanvasRenderer
{
    private const int ZoneColumns = 30;
    private const int ZoneRows = 30;
    private const int DefaultMapWidth = 21000;
    private const int DefaultMapHeight = 13500;

    public static void Render(
        SKCanvas canvas,
        int width,
        int height,
        BotUiSnapshot? snapshot,
        IReadOnlyList<MapZoneCell>? zones,
        IReadOnlyList<HeroTrailPoint> heroTrail,
        MapMoveTarget? moveTarget,
        long renderTimestamp,
        long trailLifetimeTicks)
    {
        var mapWidth = Math.Max(snapshot?.MapWidth ?? DefaultMapWidth, 1);
        var mapHeight = Math.Max(snapshot?.MapHeight ?? DefaultMapHeight, 1);
        var transform = MapViewTransform.Create(new System.Windows.Size(width, height), mapWidth, mapHeight);
        var mapRect = transform.MapRect;

        canvas.Clear(MapCanvasPalette.Background);

        DrawZones(canvas, mapRect, zones ?? []);

        var mapId = snapshot?.MapId ?? -1;
        var isLoading = mapId == -1;

        if (!isLoading)
            DrawHeroTrail(canvas, transform, heroTrail, renderTimestamp, trailLifetimeTicks);

        if (!isLoading && snapshot?.Portals is { Count: > 0 } portals)
            DrawPortals(canvas, transform, portals);

        if (snapshot?.HeroOnMap == true)
        {
            var heroPoint = transform.GameToScreen(snapshot.HeroX, snapshot.HeroY);
            DrawHero(canvas, heroPoint);

            if (moveTarget is { } target)
                DrawMoveTarget(canvas, transform, heroPoint, target);
        }

        DrawMapOverlay(canvas, width, height, isLoading, snapshot);
    }

    private static void DrawPortals(
        SKCanvas canvas,
        MapViewTransform transform,
        IReadOnlyList<MapPortalSnapshot> portals)
    {
        var radius = MapCanvasPalette.PortalSizePx / 2f;

        using var portalRing = new SKPaint
        {
            Color = MapCanvasPalette.Portals,
            IsStroke = true,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        using var portalFont = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var portalLabelPaint = new SKPaint
        {
            Color = MapCanvasPalette.TextDark,
            IsAntialias = true
        };

        foreach (var portal in portals)
        {
            var portalPoint = transform.GameToScreen(portal.X, portal.Y);
            canvas.DrawCircle(portalPoint, radius, portalRing);
            canvas.DrawText(
                portal.TargetLabel,
                portalPoint.X,
                portalPoint.Y - MapCanvasPalette.PortalLabelOffsetPx,
                SKTextAlign.Center,
                portalFont,
                portalLabelPaint);
        }
    }

    private static void DrawMoveTarget(
        SKCanvas canvas,
        MapViewTransform transform,
        SKPoint heroPoint,
        MapMoveTarget target)
    {
        var targetPoint = transform.GameToScreen(target.GameX, target.GameY);

        using var linePaint = new SKPaint
        {
            Color = MapCanvasPalette.Going,
            IsStroke = true,
            StrokeWidth = MapCanvasPalette.MoveLineStrokePx,
            IsAntialias = true
        };
        canvas.DrawLine(heroPoint, targetPoint, linePaint);
    }

    private static void DrawZones(SKCanvas canvas, SKRect mapRect, IReadOnlyList<MapZoneCell> zones)
    {
        if (zones.Count == 0)
            return;

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

            fillPaint.Color = GetZoneFillColor(zone.Kind);
            strokePaint.Color = GetZoneStrokeColor(zone.Kind);
            canvas.DrawRect(zoneRect, fillPaint);
            canvas.DrawRect(zoneRect, strokePaint);
        }
    }

    private static void DrawHeroTrail(
        SKCanvas canvas,
        MapViewTransform transform,
        IReadOnlyList<HeroTrailPoint> heroTrail,
        long renderTimestamp,
        long trailLifetimeTicks)
    {
        if (heroTrail.Count < 2)
            return;

        using var trailPaint = new SKPaint
        {
            IsStroke = true,
            StrokeWidth = MapCanvasPalette.TrailStrokePx,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };

        var hasPreviousPoint = false;
        var previousPoint = default(SKPoint);
        foreach (var trailPoint in heroTrail)
        {
            var screenPoint = transform.GameToScreen(trailPoint.X, trailPoint.Y);
            if (!hasPreviousPoint)
            {
                previousPoint = screenPoint;
                hasPreviousPoint = true;
                continue;
            }

            var age = Math.Clamp((float)(renderTimestamp - trailPoint.Timestamp) / trailLifetimeTicks, 0, 1);
            var alpha = (byte)Math.Clamp(255 * (1 - age), 0, 255);
            if (alpha > 8)
            {
                trailPaint.Color = MapCanvasPalette.TrailBase.WithAlpha(alpha);
                canvas.DrawLine(previousPoint, screenPoint, trailPaint);
            }

            previousPoint = screenPoint;
        }
    }

    private static void DrawHero(SKCanvas canvas, SKPoint heroPoint)
    {
        var radius = MapCanvasPalette.HeroSizePx / 2f;

        using var heroPaint = new SKPaint
        {
            Color = MapCanvasPalette.Hero,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(heroPoint, radius, heroPaint);
    }

    private static void DrawMapOverlay(
        SKCanvas canvas,
        int width,
        int height,
        bool isLoading,
        BotUiSnapshot? snapshot)
    {
        var centerX = width / 2f;
        var centerY = height / 2f;
        var mapName = snapshot?.MapName ?? "Загрузка";

        if (isLoading)
        {
            using var loadingFont = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontBigPx);
            using var loadingPaint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
            canvas.DrawText(mapName, centerX, centerY, SKTextAlign.Center, loadingFont, loadingPaint);
            return;
        }

        using var mapNameFont = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontBigPx);
        using var mapNamePaint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        canvas.DrawText(mapName, centerX, centerY - 5, SKTextAlign.Center, mapNameFont, mapNamePaint);

        var status = snapshot?.BotRunning == true ? "RUNNING" : "WAITING";
        using var statusFont = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var statusPaint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        canvas.DrawText(status, centerX, centerY + 35, SKTextAlign.Center, statusFont, statusPaint);

        if (snapshot is { HeroOnMap: true })
        {
            using var coordFont = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
            using var coordPaint = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
            canvas.DrawText(
                $"X {(int)snapshot.HeroX}  Y {(int)snapshot.HeroY}",
                12,
                height - 12,
                SKTextAlign.Left,
                coordFont,
                coordPaint);
        }

        if (snapshot is { HeroValid: true, HeroMaxHp: > 0 })
            DrawHeroHealthBar(canvas, width, height, snapshot.HeroHp, snapshot.HeroMaxHp);
    }

    private static void DrawHeroHealthBar(SKCanvas canvas, int width, int height, int hp, int maxHp)
    {
        const int marginX = 10;
        const int barHeight = 12;
        const int bottomOffset = 34;
        var barWidth = width / 2 - 20;
        if (barWidth <= 0)
            return;

        var left = marginX;
        var top = height - bottomOffset;
        var fillWidth = barWidth * Math.Clamp((float)hp / maxHp, 0f, 1f);

        using var framePaint = new SKPaint
        {
            Color = MapCanvasPalette.HealthDark,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var fillPaint = new SKPaint
        {
            Color = MapCanvasPalette.Health,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRect(left, top, barWidth, barHeight, framePaint);
        if (fillWidth > 0)
            canvas.DrawRect(left, top, fillWidth, barHeight, fillPaint);
    }

    private static SKColor GetZoneFillColor(MapZoneKind kind) =>
        kind switch
        {
            MapZoneKind.Preferred => new SKColor(0, 255, 128, 32),
            MapZoneKind.Forbidden => new SKColor(255, 0, 0, 32),
            MapZoneKind.Safe => new SKColor(16, 128, 255, 96),
            _ => new SKColor(139, 124, 255, 70)
        };

    private static SKColor GetZoneStrokeColor(MapZoneKind kind) =>
        kind switch
        {
            MapZoneKind.Preferred => new SKColor(0, 255, 128, 96),
            MapZoneKind.Forbidden => new SKColor(255, 0, 0, 96),
            MapZoneKind.Safe => new SKColor(16, 128, 255, 160),
            _ => new SKColor(139, 124, 255, 150)
        };
}
