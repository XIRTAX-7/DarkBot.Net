namespace DarkBot.Net.Application.Mappers.Bot;

internal static class StatsDisplayMapper
{
    public static double ToEarnedPerHour(double earned, TimeSpan runtime)
    {
        var seconds = Math.Max((long)runtime.TotalSeconds, 1L);
        return earned / (seconds / 3600d);
    }
}
