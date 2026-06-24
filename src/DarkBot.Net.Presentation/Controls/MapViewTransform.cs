using System.Windows;
using DarkBot.Net.Core.Game;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls;

/// <summary>
/// Преобразование экран ↔ игровые координаты. Как в Java MapGraphicsImpl:
/// карта растягивается на весь виджет (scaleX/scaleY независимо).
/// </summary>
internal readonly struct MapViewTransform
{
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
        var controlWidth = Math.Max((float)controlSize.Width, 1f);
        var controlHeight = Math.Max((float)controlSize.Height, 1f);

        return new MapViewTransform
        {
            MapRect = new SKRect(0, 0, controlWidth, controlHeight),
            MapWidth = width,
            MapHeight = height
        };
    }

    public bool TryScreenToMap(Point screenPoint, out double gameX, out double gameY)
    {
        gameX = 0;
        gameY = 0;

        if (MapRect.Width <= 0 || MapRect.Height <= 0)
            return false;

        var rawX = screenPoint.X * ScaleX;
        var rawY = screenPoint.Y * ScaleY;
        (gameX, gameY) = MapCoordinateBounds.Clamp(rawX, rawY, MapWidth, MapHeight);
        return true;
    }

    public SKPoint GameToScreen(double gameX, double gameY)
    {
        var (clampedX, clampedY) = MapCoordinateBounds.Clamp(gameX, gameY, MapWidth, MapHeight);
        return new SKPoint(
            (float)(clampedX / MapWidth * MapRect.Width),
            (float)(clampedY / MapHeight * MapRect.Height));
    }

    public float ToScreenSizeW(double gameW) => (float)(gameW / ScaleX);
    public float ToScreenSizeH(double gameH) => (float)(gameH / ScaleY);
}

internal readonly record struct MapMoveTarget(double GameX, double GameY, long CreatedTimestamp);
