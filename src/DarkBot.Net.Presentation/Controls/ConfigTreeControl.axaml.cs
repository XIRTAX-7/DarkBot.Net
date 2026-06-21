using Avalonia.Markup.Xaml;
using DarkBot.Net.Presentation.ViewModels;
using ReactiveUI.Avalonia;

namespace DarkBot.Net.Presentation.Controls;

public partial class ConfigTreeControl : ReactiveUserControl<ConfigTreeViewModel>
{
    public ConfigTreeControl()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
