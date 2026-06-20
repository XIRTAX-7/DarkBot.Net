using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using DarkBot.Net.Ui.Services;
using SkiaSharp;

namespace DarkBot.Net.Ui.Controls;

public sealed class MapCanvasControl : Control
{
    public static readonly StyledProperty<BotUiSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<MapCanvasControl, BotUiSnapshot?>(nameof(Snapshot));

    public BotUiSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    static MapCanvasControl()
    {
        SnapshotProperty.Changed.AddClassHandler<MapCanvasControl>((control, _) => control.InvalidateVisual());
        AffectsRender<MapCanvasControl>(SnapshotProperty);
    }

    public event EventHandler? MapClicked;

    public MapCanvasControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        topLevel?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    protected override void OnPointerPressed(Avalonia.Input.PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            MapClicked?.Invoke(this, EventArgs.Empty);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.Custom(new MapDrawOperation(bounds, Snapshot));
    }

    private sealed class MapDrawOperation : ICustomDrawOperation
    {
        private readonly BotUiSnapshot? _snapshot;

        public MapDrawOperation(Rect bounds, BotUiSnapshot? snapshot)
        {
            Bounds = bounds;
            _snapshot = snapshot;
        }

        public Rect Bounds { get; }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            if (canvas is null)
                return;

            var width = (float)Bounds.Width;
            var height = (float)Bounds.Height;
            canvas.Clear(new SKColor(24, 28, 36));

            using var borderPaint = new SKPaint
            {
                Color = new SKColor(70, 80, 100),
                IsStroke = true,
                StrokeWidth = 2,
                IsAntialias = true
            };
            canvas.DrawRect(1, 1, width - 2, height - 2, borderPaint);

            var mapWidth = _snapshot?.MapWidth ?? 21000;
            var mapHeight = _snapshot?.MapHeight ?? 13500;
            var scale = Math.Min((width - 20) / mapWidth, (height - 20) / mapHeight);
            var offsetX = (width - mapWidth * scale) / 2f;
            var offsetY = (height - mapHeight * scale) / 2f;

            using var gridPaint = new SKPaint
            {
                Color = new SKColor(45, 52, 64),
                IsStroke = true,
                StrokeWidth = 1,
                IsAntialias = true
            };

            const int gridStep = 3000;
            for (var gx = 0; gx <= mapWidth; gx += gridStep)
            {
                var x = offsetX + gx * scale;
                canvas.DrawLine(x, offsetY, x, offsetY + mapHeight * scale, gridPaint);
            }

            for (var gy = 0; gy <= mapHeight; gy += gridStep)
            {
                var y = offsetY + gy * scale;
                canvas.DrawLine(offsetX, y, offsetX + mapWidth * scale, y, gridPaint);
            }

            if (_snapshot?.HeroValid == true)
            {
                var heroX = offsetX + (float)_snapshot.HeroX * scale;
                var heroY = offsetY + (float)_snapshot.HeroY * scale;

                using var heroPaint = new SKPaint
                {
                    Color = _snapshot.BotRunning ? new SKColor(80, 220, 120) : new SKColor(240, 200, 80),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawCircle(heroX, heroY, 8, heroPaint);

                using var heroRing = new SKPaint
                {
                    Color = SKColors.White,
                    IsStroke = true,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawCircle(heroX, heroY, 10, heroRing);
            }

            using var labelPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };
            using var labelFont = new SKFont { Size = 14 };
            var mapLabel = _snapshot is null ? "Map" : $"{_snapshot.MapName} ({_snapshot.MapId})";
            canvas.DrawText(mapLabel, 8, 20, SKTextAlign.Left, labelFont, labelPaint);

            if (_snapshot is { HeroValid: true })
            {
                var hpLabel = $"HP {_snapshot.HeroHp}/{_snapshot.HeroMaxHp}";
                canvas.DrawText(hpLabel, 8, 38, SKTextAlign.Left, labelFont, labelPaint);
            }
        }

        public bool HitTest(Point p) => Bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose() { }
    }
}
