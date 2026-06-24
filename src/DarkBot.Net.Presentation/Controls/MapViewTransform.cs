using System.Windows;
using DarkBot.Net.Core.Game;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls;

/// <summary>
/// Единое преобразование экран ↔ игровые координаты карты с учётом letterbox-масштаба.
/// </summary>
internal readonly struct MapViewTransform
{
    private const float MapPadding = 18f;
    private const int MinMapDimension = 100;

    public SKRect MapRect { get; init; }
    public int MapWidth { get; init; }
    public int MapHeight { get; init; }

    public double ScaleX => MapWidth / MapRect.Width;
    public double ScaleY => MapHeight / MapRect.Height;

    public static bool HasValidMapSize(int mapWidth, int mapHeight) =>
        mapWidth >= MinMapDimension && mapHeight >= MinMapDimension;

    public static MapViewTransform Create(Size controlSize, int mapWidth, int mapHeight)
    {
        var width = Math.Max(mapWidth, 1);
        var height = Math.Max(mapHeight, 1);

        return new MapViewTransform
        {
            MapRect = CalculateMapRect((float)controlSize.Width, (float)controlSize.Height, width, height),
            MapWidth = width,
            MapHeight = height
        };
    }

    public bool TryScreenToMap(Point screenPoint, out double gameX, out double gameY)
    {
        gameX = 0;
        gameY = 0;

        if (!MapRect.Contains((float)screenPoint.X, (float)screenPoint.Y))
            return false;

        var rawX = (screenPoint.X - MapRect.Left) * ScaleX;
        var rawY = (screenPoint.Y - MapRect.Top) * ScaleY;
        (gameX, gameY) = MapCoordinateBounds.Clamp(rawX, rawY, MapWidth, MapHeight);
        return true;
    }

    public SKPoint GameToScreen(double gameX, double gameY)
    {
        var (clampedX, clampedY) = MapCoordinateBounds.Clamp(gameX, gameY, MapWidth, MapHeight);
        return new SKPoint(
            MapRect.Left + (float)(clampedX / MapWidth * MapRect.Width),
            MapRect.Top + (float)(clampedY / MapHeight * MapRect.Height));
    }

    private static SKRect CalculateMapRect(float width, float height, int mapWidth, int mapHeight)
    {
        var availableWidth = Math.Max(width - MapPadding * 2, 1);
        var availableHeight = Math.Max(height - MapPadding * 2, 1);
        var scale = Math.Min(availableWidth / mapWidth, availableHeight / mapHeight);
        var drawWidth = mapWidth * scale;
        var drawHeight = mapHeight * scale;
        var left = (width - drawWidth) / 2f;
        var top = (height - drawHeight) / 2f;

        return new SKRect(left, top, left + drawWidth, top + drawHeight);
    }
}

internal readonly record struct MapMoveTarget(double GameX, double GameY, long CreatedTimestamp);
