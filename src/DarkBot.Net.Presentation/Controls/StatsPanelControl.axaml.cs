using Avalonia.Markup.Xaml;
using DarkBot.Net.Presentation.ViewModels;
using ReactiveUI.Avalonia;

namespace DarkBot.Net.Presentation.Controls;

public partial class StatsPanelControl : ReactiveUserControl<StatsPanelViewModel>
{
    public StatsPanelControl()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
