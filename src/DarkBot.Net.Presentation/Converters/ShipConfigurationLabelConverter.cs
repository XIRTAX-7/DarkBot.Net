using System.Globalization;
using System.Windows.Data;

namespace DarkBot.Net.Presentation.Converters;

/// <summary>Форматирует активный слот корабля (1/2) как «1С» / «2С».</summary>
public sealed class ShipConfigurationLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            "1" or 1 => "1С",
            "2" or 2 => "2С",
            null or "" => "—",
            _ => value.ToString() ?? "—"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
