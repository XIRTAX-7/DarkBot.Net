using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Presentation.Services;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace DarkBot.Net.Presentation.Controls;

public sealed class MapCanvasControl : SKElement
{
    private const int MaxTrailPoints = 28;
    private const double MinTrailPointDistance = 180;
    private static readonly long TrailLifetimeTicks = Stopwatch.Frequency * 3;
    private static readonly long MoveTargetLifetimeTicks = Stopwatch.Frequency * 8;
    private const double MoveTargetArrivalDistance = 350;

    private readonly Queue<HeroTrailPoint> _heroTrail = [];
    private HeroTrailPoint[] _heroTrailSnapshot = [];
    private HeroTrailPoint? _lastTrailPoint;
    private int _trailMapId = -1;
    private MapMoveTarget? _moveTarget;

    public static readonly DependencyProperty SnapshotProperty =
        DependencyProperty.Register(
            nameof(Snapshot),
            typeof(BotUiSnapshot),
            typeof(MapCanvasControl),
            new PropertyMetadata(null, OnSnapshotChanged));

    public static readonly DependencyProperty ZonesProperty =
        DependencyProperty.Register(
            nameof(Zones),
            typeof(IReadOnlyList<MapZoneCell>),
            typeof(MapCanvasControl),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    public BotUiSnapshot? Snapshot
    {
        get => (BotUiSnapshot?)GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public IReadOnlyList<MapZoneCell>? Zones
    {
        get => (IReadOnlyList<MapZoneCell>?)GetValue(ZonesProperty);
        set => SetValue(ZonesProperty, value);
    }

    public event EventHandler<MapClickEventArgs>? MapClicked;

    public MapCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MapCanvasControl control)
            control.InvalidateVisual();
    }

    private static void OnSnapshotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MapCanvasControl control)
            return;

        control.RecordHeroTrailPoint(e.NewValue as BotUiSnapshot);
        control.UpdateMoveTarget(e.NewValue as BotUiSnapshot);
        control.InvalidateVisual();
    }

    public static bool TryScreenToGame(
        Point screenPoint,
        Size controlSize,
        BotUiSnapshot? snapshot,
        out double gameX,
        out double gameY)
    {
        gameX = 0;
        gameY = 0;

        if (snapshot is null || snapshot.MapId < 0)
            return false;

        if (!MapViewTransform.HasValidMapSize(snapshot.MapWidth, snapshot.MapHeight))
            return false;

        var transform = MapViewTransform.Create(controlSize, snapshot.MapWidth, snapshot.MapHeight);
        return transform.TryScreenToMap(screenPoint, out gameX, out gameY);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var screenPoint = e.GetPosition(this);
        if (!TryScreenToGame(screenPoint, RenderSize, Snapshot, out var gameX, out var gameY))
            return;

        _moveTarget = new MapMoveTarget(gameX, gameY, Stopwatch.GetTimestamp());
        InvalidateVisual();
        MapClicked?.Invoke(this, new MapClickEventArgs(
            gameX, gameY,
            screenPoint.X, screenPoint.Y,
            Snapshot?.HeroX ?? 0, Snapshot?.HeroY ?? 0,
            Snapshot?.MapWidth ?? 0, Snapshot?.MapHeight ?? 0));
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        PruneHeroTrail(Stopwatch.GetTimestamp());
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear(new SKColor(8, 12, 21));

        MapCanvasRenderer.Render(
            canvas,
            info.Width,
            info.Height,
            Snapshot,
            Zones,
            _heroTrailSnapshot,
            _moveTarget,
            Stopwatch.GetTimestamp(),
            TrailLifetimeTicks);
    }

    private void UpdateMoveTarget(BotUiSnapshot? snapshot)
    {
        if (_moveTarget is not { } target || snapshot is not { HeroOnMap: true, MapId: >= 0 })
            return;

        var now = Stopwatch.GetTimestamp();
        if (now - target.CreatedTimestamp > MoveTargetLifetimeTicks)
        {
            _moveTarget = null;
            return;
        }

        var dx = snapshot.HeroX - target.GameX;
        var dy = snapshot.HeroY - target.GameY;
        if (Math.Sqrt(dx * dx + dy * dy) < MoveTargetArrivalDistance)
            _moveTarget = null;
    }

    private void RecordHeroTrailPoint(BotUiSnapshot? snapshot)
    {
        var now = Stopwatch.GetTimestamp();
        PruneHeroTrail(now);

        if (snapshot is not { HeroOnMap: true, MapId: >= 0 })
            return;

        if (_trailMapId != snapshot.MapId)
        {
            _heroTrail.Clear();
            _heroTrailSnapshot = [];
            _lastTrailPoint = null;
            _trailMapId = snapshot.MapId;
        }

        var point = new HeroTrailPoint((float)snapshot.HeroX, (float)snapshot.HeroY, now);
        if (_lastTrailPoint is { } lastPoint && Distance(lastPoint, point) < MinTrailPointDistance)
            return;

        _heroTrail.Enqueue(point);
        _lastTrailPoint = point;
        while (_heroTrail.Count > MaxTrailPoints)
            _heroTrail.Dequeue();

        _heroTrailSnapshot = _heroTrail.ToArray();
    }

    private void PruneHeroTrail(long now)
    {
        var changed = false;
        while (_heroTrail.TryPeek(out var point) && now - point.Timestamp > TrailLifetimeTicks)
        {
            _heroTrail.Dequeue();
            changed = true;
        }

        if (!changed)
            return;

        _heroTrailSnapshot = _heroTrail.ToArray();
        if (_heroTrail.Count == 0)
            _lastTrailPoint = null;
    }

    private static double Distance(HeroTrailPoint first, HeroTrailPoint second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

internal static class MapCanvasRenderer
{
    private const int DefaultMapWidth = 21000;
    private const int DefaultMapHeight = 13500;
    private const int ZoneColumns = 30;
    private const int ZoneRows = 30;

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
        var transform = MapViewTransform.Create(new Size(width, height), mapWidth, mapHeight);
        var mapRect = transform.MapRect;
        var scale = mapRect.Width / mapWidth;

        using var surfacePaint = new SKPaint
        {
            Color = new SKColor(17, 26, 36),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(mapRect, 18, 18, surfacePaint);
        DrawZones(canvas, mapRect, zones ?? []);

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(58, 72, 96),
            IsStroke = true,
            StrokeWidth = 1.4f,
            IsAntialias = true
        };
        canvas.DrawRoundRect(mapRect, 18, 18, borderPaint);

        var mapId = snapshot?.MapId ?? -1;
        var isLoading = mapId == -1;

        if (!isLoading)
            DrawHeroTrail(canvas, transform, scale, heroTrail, renderTimestamp, trailLifetimeTicks);

        if (!isLoading && snapshot?.Portals is { Count: > 0 } portals)
            DrawPortals(canvas, transform, mapRect, scale, portals);

        if (snapshot?.HeroOnMap == true)
        {
            var heroPoint = transform.GameToScreen(snapshot.HeroX, snapshot.HeroY);
            DrawHero(canvas, heroPoint, scale, snapshot.BotRunning);

            if (moveTarget is { } target)
                DrawMoveTarget(canvas, transform, heroPoint, target, scale);
        }

        DrawMapLabels(canvas, mapRect, isLoading, snapshot, moveTarget);
    }

    private static void DrawPortals(
        SKCanvas canvas,
        MapViewTransform transform,
        SKRect mapRect,
        float scale,
        IReadOnlyList<MapPortalSnapshot> portals)
    {
        var portalRadius = ScaleLength(170, scale, 5, 10);
        var portalStroke = ScaleLength(36, scale, 1.3f, 2.4f);
        var portalFontSize = ScaleLength(270, scale, 10, 13);

        using var portalRing = new SKPaint
        {
            Color = new SKColor(100, 185, 255),
            IsStroke = true,
            StrokeWidth = portalStroke,
            IsAntialias = true
        };
        using var portalFont = new SKFont { Size = portalFontSize };
        using var portalLabelPaint = new SKPaint
        {
            Color = new SKColor(166, 198, 228),
            IsAntialias = true
        };

        foreach (var portal in portals)
        {
            var portalPoint = transform.GameToScreen(portal.X, portal.Y);
            if (!mapRect.Contains(portalPoint.X, portalPoint.Y))
                continue;

            canvas.DrawCircle(portalPoint, portalRadius, portalRing);
            canvas.DrawText(
                portal.TargetLabel,
                portalPoint.X,
                portalPoint.Y - portalRadius - 7,
                SKTextAlign.Center,
                portalFont,
                portalLabelPaint);
        }
    }

    private static void DrawMoveTarget(
        SKCanvas canvas,
        MapViewTransform transform,
        SKPoint heroPoint,
        MapMoveTarget target,
        float scale)
    {
        var targetPoint = transform.GameToScreen(target.GameX, target.GameY);
        var strokeWidth = Math.Clamp(80 * scale, 1.8f, 3.5f);

        using var linePaint = new SKPaint
        {
            Color = new SKColor(100, 185, 255, 200),
            IsStroke = true,
            StrokeWidth = strokeWidth,
            PathEffect = SKPathEffect.CreateDash([12, 8], 0),
            IsAntialias = true
        };
        canvas.DrawLine(heroPoint, targetPoint, linePaint);

        var targetRadius = Math.Clamp(220 * scale, 6f, 12f);
        using var ringPaint = new SKPaint
        {
            Color = new SKColor(100, 185, 255),
            IsStroke = true,
            StrokeWidth = Math.Clamp(40 * scale, 1.5f, 2.8f),
            IsAntialias = true
        };
        using var fillPaint = new SKPaint
        {
            Color = new SKColor(100, 185, 255, 90),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(targetPoint, targetRadius, fillPaint);
        canvas.DrawCircle(targetPoint, targetRadius, ringPaint);
    }

    private static float ScaleLength(float gameUnits, float scale, float min, float max) =>
        Math.Clamp(gameUnits * scale, min, max);

    private static void DrawZones(SKCanvas canvas, SKRect mapRect, IReadOnlyList<MapZoneCell> zones)
    {
        if (zones.Count == 0)
            return;

        var zoneWidth = mapRect.Width / ZoneColumns;
        var zoneHeight = mapRect.Height / ZoneRows;

        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var strokePaint = new SKPaint { IsStroke = true, StrokeWidth = 1.2f, IsAntialias = true };

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
        float scale,
        IReadOnlyList<HeroTrailPoint> heroTrail,
        long renderTimestamp,
        long trailLifetimeTicks)
    {
        if (heroTrail.Count < 2)
            return;

        var strokeWidth = ScaleLength(55, scale, 1.4f, 3.2f);
        using var trailPaint = new SKPaint
        {
            IsStroke = true,
            StrokeWidth = strokeWidth,
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
            var alpha = (byte)Math.Clamp(185 * (1 - age), 0, 185);
            if (alpha > 8)
            {
                trailPaint.Color = SKColors.White.WithAlpha(alpha);
                canvas.DrawLine(previousPoint, screenPoint, trailPaint);
            }

            previousPoint = screenPoint;
        }
    }

    private static void DrawHero(SKCanvas canvas, SKPoint heroPoint, float scale, bool isRunning)
    {
        var heroRadius = ScaleLength(180, scale, 5.5f, 11);
        var heroColor = isRunning ? new SKColor(53, 211, 155) : new SKColor(247, 201, 72);

        using var glowPaint = new SKPaint
        {
            Color = heroColor.WithAlpha(60),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(heroPoint, heroRadius * 2.2f, glowPaint);

        using var heroPaint = new SKPaint
        {
            Color = heroColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(heroPoint, heroRadius, heroPaint);

        using var heroRing = new SKPaint
        {
            Color = SKColors.White.WithAlpha(220),
            IsStroke = true,
            StrokeWidth = ScaleLength(36, scale, 1.4f, 2.6f),
            IsAntialias = true
        };
        canvas.DrawCircle(heroPoint, heroRadius * 1.35f, heroRing);
    }

    private static void DrawMapLabels(
        SKCanvas canvas,
        SKRect mapRect,
        bool isLoading,
        BotUiSnapshot? snapshot,
        MapMoveTarget? moveTarget)
    {
        var mapName = snapshot?.MapName ?? "Загрузка";
        if (isLoading)
        {
            using var loadingFont = new SKFont { Size = 22 };
            using var loadingPaint = new SKPaint { Color = new SKColor(210, 220, 232), IsAntialias = true };
            canvas.DrawText(mapName, mapRect.MidX, mapRect.MidY, SKTextAlign.Center, loadingFont, loadingPaint);
            return;
        }

        using var labelFont = new SKFont { Size = 12 };
        using var labelPaint = new SKPaint { Color = new SKColor(145, 159, 180), IsAntialias = true };

        if (snapshot is { HeroOnMap: true })
        {
            canvas.DrawText($"X {snapshot.HeroX:0}  Y {snapshot.HeroY:0}", mapRect.Left + 16, mapRect.Top + 26, SKTextAlign.Left, labelFont, labelPaint);
            if (snapshot.HeroValid)
                canvas.DrawText($"HP {snapshot.HeroHp}/{snapshot.HeroMaxHp}", mapRect.Left + 16, mapRect.Top + 44, SKTextAlign.Left, labelFont, labelPaint);
        }
        else if (snapshot is { HeroValid: true })
        {
            canvas.DrawText($"HP {snapshot.HeroHp}/{snapshot.HeroMaxHp}", mapRect.Left + 16, mapRect.Top + 26, SKTextAlign.Left, labelFont, labelPaint);
        }

        if (moveTarget is { } target)
        {
            canvas.DrawText(
                $"Target X {target.GameX:0}  Y {target.GameY:0}",
                mapRect.Right - 16,
                mapRect.Bottom - 16,
                SKTextAlign.Right,
                labelFont,
                labelPaint);
        }
    }

    private static SKColor GetZoneFillColor(MapZoneKind kind) =>
        kind switch
        {
            MapZoneKind.Preferred => new SKColor(53, 211, 155, 78),
            MapZoneKind.Forbidden => new SKColor(251, 113, 133, 86),
            MapZoneKind.Safe => new SKColor(56, 189, 248, 76),
            _ => new SKColor(139, 124, 255, 70)
        };

    private static SKColor GetZoneStrokeColor(MapZoneKind kind) =>
        kind switch
        {
            MapZoneKind.Preferred => new SKColor(53, 211, 155, 160),
            MapZoneKind.Forbidden => new SKColor(251, 113, 133, 170),
            MapZoneKind.Safe => new SKColor(56, 189, 248, 160),
            _ => new SKColor(139, 124, 255, 150)
        };
}

public readonly record struct MapZoneCell(int Column, int Row, MapZoneKind Kind);

public readonly record struct HeroTrailPoint(float X, float Y, long Timestamp);

public enum MapZoneKind
{
    Preferred,
    Forbidden,
    Safe
}

public sealed class MapClickEventArgs(
    double gameX,
    double gameY,
    double screenX,
    double screenY,
    double heroX,
    double heroY,
    int mapWidth,
    int mapHeight) : EventArgs
{
    public double GameX { get; } = gameX;
    public double GameY { get; } = gameY;
    public double ScreenX { get; } = screenX;
    public double ScreenY { get; } = screenY;
    public double HeroX { get; } = heroX;
    public double HeroY { get; } = heroY;
    public int MapWidth { get; } = mapWidth;
    public int MapHeight { get; } = mapHeight;
}
