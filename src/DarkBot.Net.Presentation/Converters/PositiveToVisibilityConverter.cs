using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DarkBot.Net.Presentation.Converters;

/// <summary>Показывает элемент, если числовое значение больше нуля.</summary>
public sealed class PositiveToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        IsPositive(value) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool IsPositive(object? value) =>
        value switch
        {
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            float f => f > 0,
            _ => false
        };
}
