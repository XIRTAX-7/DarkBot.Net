using System.Globalization;

namespace DarkBot.Net.Presentation.Formatting;

internal static class StatsDisplayFormat
{
    public static string FormatRunningTime(TimeSpan runtime)
    {
        var totalSeconds = (int)runtime.TotalSeconds;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;

        if (hours > 0)
            return string.Create(CultureInfo.InvariantCulture, $"{hours:D2}:{minutes:D2}:{seconds:D2}");

        if (minutes > 0)
            return string.Create(CultureInfo.InvariantCulture, $"{minutes:D2}:{seconds:D2}");

        return string.Create(CultureInfo.InvariantCulture, $"{seconds:D2}");
    }
}
