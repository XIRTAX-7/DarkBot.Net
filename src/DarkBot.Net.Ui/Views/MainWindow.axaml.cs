using Avalonia.Controls;
using Avalonia.Threading;
using DarkBot.Net.Ui.Controls;
using DarkBot.Net.Ui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Ui.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly StatsPanelViewModel _statsViewModel;
    private readonly DispatcherTimer _refreshTimer;
    private ConfigWindow? _configWindow;

    public MainWindow()
    {
        _viewModel = Program.AppHost.Services.GetRequiredService<MainWindowViewModel>();
        _statsViewModel = Program.AppHost.Services.GetRequiredService<StatsPanelViewModel>();
        InitializeComponent();
        // Set child DataContext before the window inherits MainWindowViewModel to children.
        StatsPanel.DataContext = _statsViewModel;
        DataContext = _viewModel;

        MapCanvas.MapClicked += (_, _) => _viewModel.ToggleBotFromMapCommand.Execute(null);

        ConfigButton.Click += (_, _) => ShowConfigWindow();
        LoginButton.Click += (_, _) => ShowLoginWindow();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _refreshTimer.Tick += (_, _) => RefreshUi();
        _refreshTimer.Start();
        RefreshUi();
    }

    private void RefreshUi()
    {
        _viewModel.Refresh();
        _statsViewModel.Apply(_viewModel.Snapshot);
        MapCanvas.Snapshot = _viewModel.Snapshot;
    }

    private void ShowConfigWindow()
    {
        _configWindow ??= new ConfigWindow();
        _configWindow.Show();
        _configWindow.Activate();
    }

    private void ShowLoginWindow()
    {
        var login = new LoginWindow();
        login.ShowDialog(this);
        RefreshUi();
    }
}
