using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DarkBot.Net.Presentation.Converters;

public sealed class BooleanAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values.Any(v => v is not bool))
            return DependencyProperty.UnsetValue;

        var result = values.All(static v => (bool)v);
        return targetType == typeof(Visibility)
            ? result ? Visibility.Visible : Visibility.Collapsed
            : result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
