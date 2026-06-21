namespace DarkBot.Net.Core.Managers;

internal static class MapLoadValidator
{
    internal const int MaxMapId = 1000;

    internal static bool IsSaneCoordinate(double x, double y) =>
        x is >= 0 and <= 30000
        && y is >= 0 and <= 30000
        && (x > 1 || y > 1);
}
