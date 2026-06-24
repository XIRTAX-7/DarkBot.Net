using System.Windows;
using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Views;

public partial class ConfigWindow : Window
{
    private readonly ConfigTreeViewModel? _viewModel;

    public ConfigWindow()
        : this(null)
    {
    }

    public ConfigWindow(ConfigTreeViewModel? viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        WindowBackgroundManager.UpdateBackground(this, ApplicationTheme.Dark, WindowBackdropType.Mica);

        if (_viewModel is not null)
            ConfigTree.ViewModel = _viewModel;
    }
}
