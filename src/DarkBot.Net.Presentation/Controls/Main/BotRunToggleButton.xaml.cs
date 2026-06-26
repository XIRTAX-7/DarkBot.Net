using System.Windows;
using System.Windows.Controls;
using DarkBot.Net.Presentation.Resources;
using Wpf.Ui.Controls;
using UiButton = Wpf.Ui.Controls.Button;

namespace DarkBot.Net.Presentation.Controls.Main;

/// <summary>
/// Кнопка запуска/остановки бота: зелёный «Старт» и оранжевый «Стоп» (WPF-UI Success / Caution).
/// </summary>
public partial class BotRunToggleButton : UserControl
{
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning),
        typeof(bool),
        typeof(BotRunToggleButton),
        new PropertyMetadata(false, OnIsRunningChanged));

    public BotRunToggleButton()
    {
        InitializeComponent();
        ApplyVisualState(IsRunning);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public UiButton ActionButtonControl => ActionButton;

    private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BotRunToggleButton control)
            control.ApplyVisualState((bool)e.NewValue);
    }

    private void ApplyVisualState(bool isRunning)
    {
        if (ActionButton is null)
            return;

        if (isRunning)
        {
            ActionButton.Appearance = ControlAppearance.Caution;
            ActionButton.Content = UiStrings.Main_StopButton;
            ActionButton.ToolTip = UiStrings.Main_StopBotTooltip;
            ActionButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Stop24, Filled = true };
            return;
        }

        ActionButton.Appearance = ControlAppearance.Success;
        ActionButton.Content = UiStrings.Main_StartButton;
        ActionButton.ToolTip = UiStrings.Main_StartBotTooltip;
        ActionButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play24, Filled = true };
    }
}
