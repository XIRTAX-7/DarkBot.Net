using DarkBot.Net.Application.Models.Bot;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal readonly ref struct MapCanvasRenderContext
{
    public required SkiaSharp.SKCanvas Canvas { get; init; }
    public required MapViewTransform Transform { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required MapStatusSnapshot Map { get; init; }
    public required MapRenderSettings Settings { get; init; }
    public required IReadOnlyList<MapZoneCell> PreferGrid { get; init; }
    public required IReadOnlyList<MapZoneCell> AvoidGrid { get; init; }
    public required IReadOnlyList<HeroTrailPoint> HeroTrail { get; init; }
    public MapMoveTarget? MoveTarget { get; init; }
    public required long RenderTimestamp { get; init; }
    public required long TrailLifetimeTicks { get; init; }

    public bool IsLoading => Map.MapId < 0;

    public bool HasDisplayFlag(MapDisplayFlag flag) =>
        Settings.DisplayFlags.HasFlag(flag);
}
