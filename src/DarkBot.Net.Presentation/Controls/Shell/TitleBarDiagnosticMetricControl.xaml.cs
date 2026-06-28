using System.Windows;
using System.Windows.Controls;
using DarkBot.Net.Presentation.Formatting;
using DarkBot.Net.Presentation.Resources;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Controls.Shell;

public partial class TitleBarDiagnosticMetricControl : UserControl
{
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(TitleBarDiagnosticMetricControl),
            new PropertyMetadata(string.Empty, OnMetricInfoChanged));

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(TitleBarDiagnosticMetricControl));

    public static readonly DependencyProperty SymbolProperty =
        DependencyProperty.Register(nameof(Symbol), typeof(SymbolRegular), typeof(TitleBarDiagnosticMetricControl));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(TitleBarDiagnosticMetricControl),
            new PropertyMetadata(TitleBarDiagnosticsFormatter.EmptyPlaceholder, OnMetricInfoChanged));

    public static readonly DependencyProperty IsLastProperty =
        DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(TitleBarDiagnosticMetricControl));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public SymbolRegular Symbol
    {
        get => (SymbolRegular)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsLast
    {
        get => (bool)GetValue(IsLastProperty);
        set => SetValue(IsLastProperty, value);
    }

    public TitleBarDiagnosticMetricControl()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateToolTip();
    }

    private static void OnMetricInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleBarDiagnosticMetricControl control)
            control.UpdateToolTip();
    }

    private void UpdateToolTip()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            ToolTip = null;
            return;
        }

        var valueLine = Value == TitleBarDiagnosticsFormatter.EmptyPlaceholder
            ? UiStrings.TitleBar_Diagnostics_NoValue
            : UiStrings.Format("TitleBar_Diagnostics_ValueFormat", Value);

        ToolTip = $"{Description}\n\n{valueLine}";
    }
}
