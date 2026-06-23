namespace DarkBot.Net.Core.Game;

/// <summary>Границы игровой карты в map-координатах (0 … width/height).</summary>
public static class MapCoordinateBounds
{
    /// <summary>Отступ от края карты — зона радиации/бездны начинается за пределами 0…width/height.</summary>
    public const int SafeMargin = 200;

    public static bool IsInBounds(double x, double y, int mapWidth, int mapHeight) =>
        mapWidth > 0
        && mapHeight > 0
        && x >= 0
        && y >= 0
        && x <= mapWidth
        && y <= mapHeight;

    public static bool IsInSafeBounds(double x, double y, int mapWidth, int mapHeight) =>
        mapWidth > SafeMargin * 2
        && mapHeight > SafeMargin * 2
        && x >= SafeMargin
        && y >= SafeMargin
        && x <= mapWidth - SafeMargin
        && y <= mapHeight - SafeMargin;

    public static bool IsOutOfBounds(double x, double y, int mapWidth, int mapHeight) =>
        !IsInBounds(x, y, mapWidth, mapHeight);

    public static (double X, double Y) Clamp(double x, double y, int mapWidth, int mapHeight)
    {
        if (mapWidth <= 0 || mapHeight <= 0)
            return (Math.Max(0, x), Math.Max(0, y));

        return (Math.Clamp(x, 0, mapWidth), Math.Clamp(y, 0, mapHeight));
    }

    /// <summary>Кламп с отступом от краёв — цели полёта не попадают в бездну.</summary>
    public static (double X, double Y) SafeClamp(double x, double y, int mapWidth, int mapHeight)
    {
        if (mapWidth <= SafeMargin * 2 || mapHeight <= SafeMargin * 2)
            return Clamp(x, y, mapWidth, mapHeight);

        return (
            Math.Clamp(x, SafeMargin, mapWidth - SafeMargin),
            Math.Clamp(y, SafeMargin, mapHeight - SafeMargin));
    }

    public static (float X, float Y) Clamp(float x, float y, int mapWidth, int mapHeight)
    {
        if (mapWidth <= 0 || mapHeight <= 0)
            return (x, y);

        return (Math.Clamp(x, 0, mapWidth), Math.Clamp(y, 0, mapHeight));
    }
}
