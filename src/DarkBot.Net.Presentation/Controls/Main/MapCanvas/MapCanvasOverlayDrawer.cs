using DarkBot.Net.Application.Models.Bot;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasOverlayDrawer
{
    public static void Draw(MapCanvasRenderContext ctx)
    {
        if (ctx.IsLoading)
            return;

        if (ctx.HasDisplayFlag(MapDisplayFlag.GroupArea) && ctx.Map.Overlay.GroupMembers.Count > 0)
            DrawGroup(ctx);
        else if (ctx.HasDisplayFlag(MapDisplayFlag.BoosterArea))
            DrawBoosters(ctx);
    }

    private static void DrawGroup(MapCanvasRenderContext ctx)
    {
        var hideNames = !ctx.HasDisplayFlag(MapDisplayFlag.GroupNames);
        var members = ctx.Map.Overlay.GroupMembers;
        var maxWidth = members.Max(m => EstimateMemberWidth(m, hideNames));
        var panelHeight = 28 + members.Count * 26;
        var panel = LayoutPanel(ctx, panelHeight, maxWidth, SKTextAlign.Right);

        using var bg = new SKPaint { Color = MapCanvasPalette.TextsBackground, Style = SKPaintStyle.Fill };
        ctx.Canvas.DrawRect(panel, bg);

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        using var boldFont = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx)
        {
            Embolden = true
        };
        using var textPaint = new SKPaint { Color = MapCanvasPalette.Text, IsAntialias = true };
        var y = panel.Top + MapCanvasPalette.OverlayPaddingPx;
        foreach (var member in members)
        {
            var label = hideNames && member.DisplayText.Length > 2
                ? member.DisplayText[..Math.Min(2, member.DisplayText.Length)]
                : member.DisplayText;
            var activeFont = member.IsLeader ? boldFont : font;

            ctx.Canvas.DrawText(label, panel.Right - MapCanvasPalette.OverlayPaddingPx, y + 14, SKTextAlign.Right, activeFont, textPaint);

            var barLeft = panel.Left + MapCanvasPalette.OverlayPaddingPx;
            var barWidth = panel.Width - MapCanvasPalette.OverlayPaddingPx * 2;
            MapCanvasDrawHelpers.DrawHealthBar(ctx.Canvas, barLeft, y + 18, barWidth / 2 - 3, 4, member.Hp, member.MaxHp);
            if (member.HasTarget)
            {
                MapCanvasDrawHelpers.DrawHealthBar(
                    ctx.Canvas,
                    barLeft + barWidth / 2 + 3,
                    y + 18,
                    barWidth / 2 - 3,
                    4,
                    member.TargetHp,
                    member.TargetMaxHp);
            }

            y += 26;
        }
    }

    private static void DrawBoosters(MapCanvasRenderContext ctx)
    {
        var boosters = ctx.Map.Overlay.Boosters;
        if (boosters.Count == 0)
            return;

        var ordered = ctx.HasDisplayFlag(MapDisplayFlag.SortBoosters)
            ? boosters.OrderByDescending(b => b.Text.Length).ToList()
            : boosters.ToList();

        var maxWidth = ordered.Max(b => b.Text.Length * 7f);
        var panelHeight = 15 + ordered.Count * 15;
        var panel = LayoutPanel(ctx, panelHeight, maxWidth, SKTextAlign.Right);

        using var bg = new SKPaint { Color = MapCanvasPalette.TextsBackground, Style = SKPaintStyle.Fill };
        ctx.Canvas.DrawRect(panel, bg);

        using var font = new SKFont(MapCanvasPalette.ConsolasTypeface, MapCanvasPalette.FontSmallPx);
        var y = panel.Top + MapCanvasPalette.OverlayPaddingPx;
        foreach (var booster in ordered)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(
                    (byte)((booster.ColorArgb >> 16) & 0xFF),
                    (byte)((booster.ColorArgb >> 8) & 0xFF),
                    (byte)(booster.ColorArgb & 0xFF)),
                IsAntialias = true
            };
            ctx.Canvas.DrawText(
                booster.Text,
                panel.Right - MapCanvasPalette.OverlayPaddingPx,
                y + 14,
                SKTextAlign.Right,
                font,
                paint);
            y += 15;
        }
    }

    private static SKRect LayoutPanel(
        MapCanvasRenderContext ctx,
        float height,
        float contentWidth,
        SKTextAlign align)
    {
        var width = Math.Min(contentWidth + MapCanvasPalette.OverlayPaddingPx * 2, 220);
        var left = align == SKTextAlign.Right
            ? ctx.Width - width - 8
            : 8;
        return new SKRect(left, 8, left + width, 8 + height);
    }

    private static float EstimateMemberWidth(MapGroupMemberSnapshot member, bool hideNames) =>
        hideNames ? 40 : Math.Min(member.DisplayText.Length * 7f, 200);
}
