using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls;

/// <summary>Палитра и размеры карты — порт ColorScheme / MapGraphics из Java DarkBot.</summary>
internal static class MapCanvasPalette
{
    public static readonly SKColor Background = new(0x26, 0x32, 0x38);
    public static readonly SKColor Text = new(0xF2, 0xF2, 0xF2);
    public static readonly SKColor TextDark = new(0xBB, 0xBB, 0xBB);
    public static readonly SKColor Going = new(0x8F, 0x9B, 0xFF);
    public static readonly SKColor Portals = new(0xAE, 0xAE, 0xAE);
    public static readonly SKColor Hero = new(0x22, 0xCC, 0x22);
    public static readonly SKColor Health = new(0x38, 0x8E, 0x3C);
    public static readonly SKColor HealthDark = new(0x2E, 0x7D, 0x32);
    public static readonly SKColor TrailBase = new(0xE0, 0xE0, 0xE0);

    public const float HeroSizePx = 9f;
    public const float PortalSizePx = 12f;
    public const float TrailStrokePx = 1f;
    public const float MoveLineStrokePx = 1f;
    public const float PortalLabelOffsetPx = 8f;

    public const float FontBigPx = 32f;
    public const float FontMidPx = 18f;
    public const float FontSmallPx = 13f;

    public static SKTypeface ConsolasTypeface { get; } =
        SKTypeface.FromFamilyName("Cascadia Mono", SKFontStyle.Normal)
        ?? SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
        ?? SKTypeface.Default;

    public static SKTypeface SansTypeface { get; } =
        SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
        ?? SKTypeface.Default;
}
