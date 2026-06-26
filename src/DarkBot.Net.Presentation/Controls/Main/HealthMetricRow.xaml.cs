using System.Windows;
using System.Windows.Media;

namespace DarkBot.Net.Presentation.Controls.Main;

public partial class HealthMetricRow
{
    private static readonly DependencyPropertyKey TotalValuePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TotalValue),
            typeof(double),
            typeof(HealthMetricRow),
            new PropertyMetadata(0d));

    private static readonly DependencyPropertyKey TotalMaximumPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TotalMaximum),
            typeof(double),
            typeof(HealthMetricRow),
            new PropertyMetadata(1d));

    public static readonly DependencyProperty TotalValueProperty = TotalValuePropertyKey.DependencyProperty;
    public static readonly DependencyProperty TotalMaximumProperty = TotalMaximumPropertyKey.DependencyProperty;

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(HealthMetricRow));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(HealthMetricRow),
            new PropertyMetadata(0d, OnMetricChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(HealthMetricRow),
            new PropertyMetadata(1d, OnMetricChanged));

    public static readonly DependencyProperty OverflowValueProperty =
        DependencyProperty.Register(
            nameof(OverflowValue),
            typeof(double),
            typeof(HealthMetricRow),
            new PropertyMetadata(0d, OnMetricChanged));

    public static readonly DependencyProperty OverflowMaximumProperty =
        DependencyProperty.Register(
            nameof(OverflowMaximum),
            typeof(double),
            typeof(HealthMetricRow),
            new PropertyMetadata(0d, OnMetricChanged));

    public static readonly DependencyProperty FillBrushProperty =
        DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(HealthMetricRow));

    public double TotalValue => (double)GetValue(TotalValueProperty);

    public double TotalMaximum => (double)GetValue(TotalMaximumProperty);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Дополнительная прочность поверх базовой (наниты).</summary>
    public double OverflowValue
    {
        get => (double)GetValue(OverflowValueProperty);
        set => SetValue(OverflowValueProperty, value);
    }

    /// <summary>Максимум дополнительной прочности (наниты).</summary>
    public double OverflowMaximum
    {
        get => (double)GetValue(OverflowMaximumProperty);
        set => SetValue(OverflowMaximumProperty, value);
    }

    public Brush FillBrush
    {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public HealthMetricRow()
    {
        InitializeComponent();
        UpdateTotals();
    }

    private static void OnMetricChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HealthMetricRow row)
            row.UpdateTotals();
    }

    private void UpdateTotals()
    {
        SetValue(TotalValuePropertyKey, Value + OverflowValue);
        SetValue(TotalMaximumPropertyKey, Math.Max(Maximum + OverflowMaximum, 1d));
    }
}
