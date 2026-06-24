using DarkBot.Net.Presentation.Services;

namespace DarkBot.Net.Presentation.Controls;

internal readonly ref struct MapCanvasRenderContext
{
    public required SkiaSharp.SKCanvas Canvas { get; init; }
    public required MapViewTransform Transform { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required MapRenderSnapshot Map { get; init; }
    public required IReadOnlyList<HeroTrailPoint> HeroTrail { get; init; }
    public MapMoveTarget? MoveTarget { get; init; }
    public required long RenderTimestamp { get; init; }
    public required long TrailLifetimeTicks { get; init; }

    public bool IsLoading => Map.MapId < 0;
    public bool HasDisplayFlag(Services.MapDisplayFlag flag) =>
        Map.Settings.DisplayFlags.HasFlag(flag);
}
