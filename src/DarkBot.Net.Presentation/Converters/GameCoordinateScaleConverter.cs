using System.Globalization;
using System.Windows.Data;

namespace DarkBot.Net.Presentation.Converters;

/// <summary>Делит игровые координаты на 100 и показывает целое число.</summary>
public sealed class GameCoordinateScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return "—";

        if (!TryScale(value, out var scaled))
            return "—";

        return scaled.ToString(culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool TryScale(object value, out int scaled)
    {
        switch (value)
        {
            case double d:
                scaled = (int)(d / 100);
                return true;
            case float f:
                scaled = (int)(f / 100);
                return true;
            case int i:
                scaled = i / 100;
                return true;
            case long l:
                scaled = (int)(l / 100);
                return true;
            default:
                scaled = 0;
                return false;
        }
    }
}
