using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

/// <summary>Палитра и размеры карты — порт ColorScheme / MapGraphics из Java DarkBot.</summary>
internal static class MapCanvasPalette
{
    public static readonly SKColor Background = new(0x26, 0x32, 0x38);
    public static readonly SKColor Radiation = new(0x37, 0x26, 0x38);
    public static readonly SKColor Text = new(0xF2, 0xF2, 0xF2);
    public static readonly SKColor TextDark = new(0xBB, 0xBB, 0xBB);
    public static readonly SKColor Going = new(0x8F, 0x9B, 0xFF);
    public static readonly SKColor Portals = new(0xAE, 0xAE, 0xAE);
    public static readonly SKColor Hero = new(0x22, 0xCC, 0x22);
    public static readonly SKColor Health = new(0x38, 0x8E, 0x3C);
    public static readonly SKColor HealthDark = new(0x2E, 0x7D, 0x32);
    public static readonly SKColor NanoHull = new(0xD0, 0xD0, 0x24);
    public static readonly SKColor Shield = new(0x02, 0x88, 0xD1);
    public static readonly SKColor TrailBase = new(0xE0, 0xE0, 0xE0);
    public static readonly SKColor Fuel = new(0xF2, 0xF2, 0xF2);
    public static readonly SKColor Boxes = new(0xBB, 0xB8, 0x30);
    public static readonly SKColor Mines = new(0xFF, 0x80, 0x00);
    public static readonly SKColor Allies = new(0x29, 0xB6, 0xF6);
    public static readonly SKColor Enemies = new(0xD5, 0x00, 0x00);
    public static readonly SKColor Npcs = new(0xAA, 0x40, 0x40);
    public static readonly SKColor GroupMember = new(0xFF, 0xD7, 0x00);
    public static readonly SKColor LowRelays = new(0x00, 0xD5, 0x4B);
    public static readonly SKColor SpaceBalls = new(0x00, 0xD5, 0x95);
    public static readonly SKColor OtherEntities = new(0x16, 0x47, 0xA1);
    public static readonly SKColor Target = new(0x7A, 0x2D, 0x2D);
    public static readonly SKColor Pet = new(0x00, 0x4C, 0x8C);
    public static readonly SKColor PetIn = new(0xC5, 0x60, 0x00);
    public static readonly SKColor Meteoroid = new(0xAA, 0xAA, 0xAA);
    public static readonly SKColor Ping = new(0x00, 0xFF, 0x00, 0x20);
    public static readonly SKColor PingBorder = new(0x00, 0xFF, 0x00, 0x80);
    public static readonly SKColor Barrier = new(0xFF, 0xFF, 0xFF, 0x20);
    public static readonly SKColor BarrierBorder = new(0xFF, 0xFF, 0xFF, 0x80);
    public static readonly SKColor NoCloak = new(0x18, 0xA0, 0xFF, 0x20);
    public static readonly SKColor Prefer = new(0x00, 0xFF, 0x80, 0x20);
    public static readonly SKColor Avoid = new(0xFF, 0x00, 0x00, 0x20);
    public static readonly SKColor Safety = new(0x10, 0x80, 0xFF, 0x60);
    public static readonly SKColor Bases = new(0x00, 0xFF, 0x80);
    public static readonly SKColor BaseSpots = new(0x00, 0xFF, 0x80, 0x20);
    public static readonly SKColor Unknown = new(0x7C, 0x05, 0xD1);
    public static readonly SKColor TextsBackground = new(0x26, 0x32, 0x38, 0x80);
    public static readonly SKColor ActionButton = new(0xFF, 0xFF, 0xFF, 0xA0);
    public static readonly SKColor DarkenBack = new(0x00, 0x00, 0x00, 0x60);
    public static readonly SKColor MapZoomBorder = Unknown;

    public const float HeroSizePx = 9f;
    public const float PetOuterSizePx = 9f;
    public const float PetInnerSizePx = 7f;
    public const float PortalSizePx = 12f;
    public const float TrailStrokePx = 1f;
    public const float MoveLineStrokePx = 1f;
    public const float PortalLabelOffsetPx = 8f;
    public const float HealthBarHeightPx = 12f;
    public const float OverlayPaddingPx = 8f;

    public const float FontBigPx = 32f;
    public const float FontMidPx = 18f;
    public const float FontSmallPx = 13f;
    public const float FontTinyPx = 9f;

    public static SKTypeface ConsolasTypeface { get; } =
        SKTypeface.FromFamilyName("Cascadia Mono", SKFontStyle.Normal)
        ?? SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
        ?? SKTypeface.Default;

    public static SKTypeface SansTypeface { get; } =
        SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
        ?? SKTypeface.Default;

    private static SKColor[]? _trailGradient;

    /// <summary>Градиент alpha 1→255 как Java ColorScheme.getTrail().</summary>
    public static ReadOnlySpan<SKColor> TrailGradient
    {
        get
        {
            _trailGradient ??= Enumerable.Range(1, 255)
                .Select(i => TrailBase.WithAlpha((byte)i))
                .ToArray();
            return _trailGradient;
        }
    }
}
