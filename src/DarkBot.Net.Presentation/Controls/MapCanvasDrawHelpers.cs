using DarkBot.Net.Presentation.Services;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls;

internal static class MapCanvasDrawHelpers
{
    public static (float Width, float Height, bool Round) GetEntityDrawParams(MapEntitySnapshot entity, bool roundEntities)
    {
        var (baseSize, stroke) = entity.Kind switch
        {
            MapEntityKind.Box or MapEntityKind.Mine => (3f, false),
            MapEntityKind.Npc or MapEntityKind.Player or MapEntityKind.Pet or MapEntityKind.Relay => (4f, false),
            MapEntityKind.SpaceBall => (6f, false),
            MapEntityKind.Static => (2f, true),
            _ => (4f, false)
        };

        var round = roundEntities && !stroke;
        var size = entity.Fill ? baseSize + 1f : baseSize;
        if (round)
            size += 1f;

        return (size, size, round || entity.Fill);
    }

    public static SKColor ResolveEntityColor(MapEntitySnapshot entity) =>
        entity.Kind switch
        {
            MapEntityKind.Box => MapCanvasPalette.Boxes,
            MapEntityKind.Mine => MapCanvasPalette.Mines,
            MapEntityKind.Npc => MapCanvasPalette.Npcs,
            MapEntityKind.Relay => MapCanvasPalette.LowRelays,
            MapEntityKind.SpaceBall => MapCanvasPalette.SpaceBalls,
            MapEntityKind.Static => MapCanvasPalette.OtherEntities,
            MapEntityKind.Pet => MapCanvasPalette.Pet,
            MapEntityKind.Player when entity.IsGroupMember => MapCanvasPalette.GroupMember,
            MapEntityKind.Player when entity.IsEnemy => MapCanvasPalette.Enemies,
            MapEntityKind.Player => MapCanvasPalette.Allies,
            MapEntityKind.BattleStation when entity.SubKind == "asteroid" => MapCanvasPalette.Meteoroid,
            MapEntityKind.BattleStation when entity.IsEnemy => MapCanvasPalette.Enemies,
            MapEntityKind.BattleStation => MapCanvasPalette.Allies,
            MapEntityKind.StationTurret => MapCanvasPalette.Bases,
            MapEntityKind.BaseSpot => MapCanvasPalette.BaseSpots,
            MapEntityKind.Portal => MapCanvasPalette.Portals,
            _ => MapCanvasPalette.OtherEntities
        };

    public static void DrawHealthBar(
        SKCanvas canvas,
        float left,
        float top,
        float width,
        float height,
        int hp,
        int maxHp,
        int nano = 0,
        int maxNano = 0,
        int shield = 0,
        int maxShield = 0)
    {
        if (width <= 0 || height <= 0)
            return;

        using var framePaint = new SKPaint
        {
            Color = MapCanvasPalette.HealthDark,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(left, top, width, height, framePaint);

        var cursor = left;
        DrawSegment(canvas, ref cursor, top, width, height, hp, maxHp, MapCanvasPalette.Health);
        DrawSegment(canvas, ref cursor, top, width, height, nano, maxNano, MapCanvasPalette.NanoHull);
        DrawSegment(canvas, ref cursor, top, width, height, shield, maxShield, MapCanvasPalette.Shield);
    }

    private static void DrawSegment(
        SKCanvas canvas,
        ref float cursor,
        float top,
        float totalWidth,
        float height,
        int value,
        int maxValue,
        SKColor color)
    {
        if (maxValue <= 0 || value <= 0)
            return;

        var segmentWidth = totalWidth * Math.Clamp((float)value / maxValue, 0f, 1f);
        if (segmentWidth <= 0)
            return;

        using var paint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRect(cursor, top, segmentWidth, height, paint);
        cursor += segmentWidth;
    }

    public static string FormatHealthNumber(int value) =>
        value.ToString("N0").Replace(',', ' ');
}
