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
    private const int MaxTrailPoints = 128;
    private const double MinTrailPointDistance = 25;
    private const double TeleportResetDistance = 500;
    private static readonly long TrailLifetimeTicks = Stopwatch.Frequency * 15;
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

    public BotUiSnapshot? Snapshot
    {
        get => (BotUiSnapshot?)GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public event EventHandler<MapClickEventArgs>? MapClicked;

    public MapCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
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

        MapCanvasRenderer.Render(
            canvas,
            info.Width,
            info.Height,
            Snapshot,
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
        if (_lastTrailPoint is { } lastPoint)
        {
            if (Distance(lastPoint, point) >= TeleportResetDistance)
            {
                _heroTrail.Clear();
                _lastTrailPoint = null;
            }
            else if (Distance(lastPoint, point) < MinTrailPointDistance)
            {
                return;
            }
        }

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
