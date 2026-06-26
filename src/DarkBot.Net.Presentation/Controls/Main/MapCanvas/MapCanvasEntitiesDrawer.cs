using DarkBot.Net.Application.DTOs.Responses.Bot;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasEntitiesDrawer
{
    private const float LocatorPingSize = 16f;
    private const float TurretOvalDiameterPx = 2f;

    // SafetyInfo.diameter() — на карте эти зоны воспринимаются как круги безопасности базы.
    private const int HeadquarterOvalDiameter = 2500;
    private const int HomeBaseOvalDiameter = 3000;
    private const int BaseSpotOvalDiameter = 500;

    public static void DrawConstant(MapCanvasRenderContext ctx)
    {
        if (ctx.IsLoading)
            return;

        DrawPortals(ctx, ctx.Map.Entities.Portals);
        DrawBattleStations(ctx, ctx.Map.Entities.BattleStations);
        DrawStations(ctx, ctx.Map.Entities.Stations);
    }

    public static void DrawDynamic(MapCanvasRenderContext ctx)
    {
        if (ctx.IsLoading)
            return;

        var round = ctx.Settings.RoundEntities;
        DrawList(ctx, ctx.Map.Entities.Boxes, round);
        DrawList(ctx, ctx.Map.Entities.Mines, round);
        DrawList(ctx, ctx.Map.Entities.Relays, round);
        DrawList(ctx, ctx.Map.Entities.SpaceBalls, round);
        DrawList(ctx, ctx.Map.Entities.StaticEntities, round);
        DrawList(ctx, ctx.Map.Entities.Npcs, round);
        DrawList(ctx, ctx.Map.Entities.Pets, round);
        DrawList(ctx, ctx.Map.Entities.Players, round);
        DrawTarget(ctx);
    }

    private static void DrawPortals(MapCanvasRenderContext ctx, IReadOnlyList<MapEntitySnapshot> portals)
    {
        var radius = MapCanvasPalette.PortalSizePx / 2f;
        using var ring = new SKPaint
        {
            Color = MapCanvasPalette.Portals,
            IsStroke = true,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var label = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };

        foreach (var portal in portals)
        {
            var point = ctx.Transform.GameToScreen(portal.X, portal.Y);
            ctx.Canvas.DrawCircle(point, radius, ring);
            if (!string.IsNullOrEmpty(portal.Label))
            {
                ctx.Canvas.DrawText(
                    portal.Label,
                    point.X,
                    point.Y - MapCanvasPalette.PortalLabelOffsetPx,
                    SKTextAlign.Center,
                    font,
                    label);
            }
        }
    }

    private static void DrawBattleStations(MapCanvasRenderContext ctx, IReadOnlyList<MapEntitySnapshot> stations)
    {
        foreach (var bs in stations)
        {
            var color = MapCanvasDrawHelpers.ResolveEntityColor(bs);
            var point = ctx.Transform.GameToScreen(bs.X, bs.Y);
            using var paint = new SKPaint { Color = color, IsAntialias = true };

            if (bs.SubKind == "module")
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1f;
                ctx.Canvas.DrawRect(point.X - 1.5f, point.Y - 1.5f, 3f, 3f, paint);
            }
            else
            {
                paint.Style = SKPaintStyle.Fill;
                ctx.Canvas.DrawOval(point.X - 5.5f, point.Y - 4.5f, point.X + 5.5f, point.Y + 4.5f, paint);

                if (ctx.HasDisplayFlag(MapDisplayFlag.Usernames) && !string.IsNullOrEmpty(bs.Label))
                {
                    using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
                    using var text = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
                    ctx.Canvas.DrawText(bs.Label, point.X, point.Y - 14, SKTextAlign.Center, font, text);
                }
            }
        }
    }

    private static void DrawStations(MapCanvasRenderContext ctx, IReadOnlyList<MapEntitySnapshot> stations)
    {
        foreach (var station in stations)
        {
            var point = ctx.Transform.GameToScreen(station.X, station.Y);
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

            if (station.Kind == MapEntityKind.StationTurret)
            {
                paint.Color = MapCanvasPalette.Bases;
                ctx.Canvas.DrawCircle(point, TurretOvalDiameterPx / 2f, paint);
                continue;
            }

            paint.Color = MapCanvasPalette.BaseSpots;
            var diameter = station.SubKind switch
            {
                "hq" => HeadquarterOvalDiameter,
                "home" => HomeBaseOvalDiameter,
                _ => BaseSpotOvalDiameter
            };
            var rx = ctx.Transform.ToScreenSizeW(diameter) / 2f;
            var ry = ctx.Transform.ToScreenSizeH(diameter) / 2f;
            ctx.Canvas.DrawOval(point.X - rx, point.Y - ry, point.X + rx, point.Y + ry, paint);
        }
    }

    private static void DrawList(MapCanvasRenderContext ctx, IReadOnlyList<MapEntitySnapshot> entities, bool round)
    {
        foreach (var entity in entities)
            DrawEntity(ctx, entity, round);
    }

    private static void DrawEntity(MapCanvasRenderContext ctx, MapEntitySnapshot entity, bool roundEntities)
    {
        var (w, h, round) = MapCanvasDrawHelpers.GetEntityDrawParams(entity, roundEntities);
        var point = ctx.Transform.GameToScreen(entity.X, entity.Y);
        using var paint = new SKPaint
        {
            Color = MapCanvasDrawHelpers.ResolveEntityColor(entity),
            IsAntialias = true,
            Style = round ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            StrokeWidth = round ? 0f : 1f
        };

        if (round)
            ctx.Canvas.DrawCircle(point, Math.Max(w, h) / 2f, paint);
        else
            ctx.Canvas.DrawRect(point.X - w / 2f, point.Y - h / 2f, w, h, paint);

        DrawEntityLabel(ctx, entity, point);
    }

    private static void DrawEntityLabel(MapCanvasRenderContext ctx, MapEntitySnapshot entity, SKPoint point)
    {
        if (string.IsNullOrEmpty(entity.Label))
            return;

        var yOffset = entity.Kind switch
        {
            MapEntityKind.Npc when ctx.HasDisplayFlag(MapDisplayFlag.NpcNames) => -6f,
            MapEntityKind.Box when ctx.HasDisplayFlag(MapDisplayFlag.ResourceNames) => -5f,
            MapEntityKind.Player when ctx.HasDisplayFlag(MapDisplayFlag.Usernames) => -6f,
            _ => 0f
        };
        if (yOffset == 0)
            return;

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var text = new SKPaint { Color = MapCanvasPalette.TextDark, IsAntialias = true };
        ctx.Canvas.DrawText(entity.Label, point.X, point.Y + yOffset, SKTextAlign.Center, font, text);
    }

    private static void DrawTarget(MapCanvasRenderContext ctx)
    {
        var target = ctx.Map.Target;
        if (target is not { Id: > 0 })
            return;

        var point = ctx.Transform.GameToScreen(target.X, target.Y);
        using var fill = new SKPaint
        {
            Color = MapCanvasPalette.Target,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        ctx.Canvas.DrawCircle(point, 2.5f, fill);

        using var pingFill = new SKPaint { Color = MapCanvasPalette.Ping, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var pingStroke = new SKPaint
        {
            Color = MapCanvasPalette.PingBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        ctx.Canvas.DrawCircle(point, LocatorPingSize / 2f, pingFill);
        ctx.Canvas.DrawCircle(point, LocatorPingSize / 2f, pingStroke);
    }
}
