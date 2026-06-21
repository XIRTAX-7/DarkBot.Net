using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.ViewModels;

namespace DarkBot.Net.Presentation.Views;

public partial class ConfigWindow : Window
{
    private ConfigTreeViewModel? _viewModel;

    public ConfigWindow()
        : this(null)
    {
    }

    public ConfigWindow(ConfigTreeViewModel? viewModel)
    {
        _viewModel = viewModel;
        AvaloniaXamlLoader.Load(this);
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;

        if (_viewModel is null)
            return;

        var configTree = this.FindControl<ConfigTreeControl>("ConfigTree");
        if (configTree is not null)
            configTree.ViewModel = _viewModel;
    }
}
