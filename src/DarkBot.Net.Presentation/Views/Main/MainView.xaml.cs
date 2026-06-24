using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.Services;
using DarkBot.Net.Presentation.ViewModels;
using DarkBot.Net.Presentation.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace DarkBot.Net.Presentation.Views.Main;

public partial class MainView : ReactiveUserControl<MainWindowViewModel>
{
    private StatsPanelViewModel? _statsViewModel;
    private DispatcherTimer? _refreshTimer;
    private ConfigWindow? _configWindow;

    public MainView()
    {
        Log.Information("UI view: MainView created");
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (Program.AppHost is null || ViewModel is null)
                return;

            _statsViewModel = Program.AppHost.Services.GetRequiredService<StatsPanelViewModel>();
            StatsPanel.ViewModel = _statsViewModel;

            MapCanvas.MapClicked += OnMapClicked;
            disposables.Add(Disposable.Create(() => MapCanvas.MapClicked -= OnMapClicked));

            ConfigButton.Click += OnConfigClick;
            LoginButton.Click += OnLoginClick;
            disposables.Add(Disposable.Create(() =>
            {
                ConfigButton.Click -= OnConfigClick;
                LoginButton.Click -= OnLoginClick;
            }));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _refreshTimer.Tick += (_, _) => RefreshUi();
            _refreshTimer.Start();
            RefreshUi();

            disposables.Add(Disposable.Create(() => _refreshTimer?.Stop()));
        });
    }

    private void OnMapClicked(object? sender, MapClickEventArgs e) =>
        ViewModel?.MoveShipToMapLocation(e);

    private void OnConfigClick(object? sender, RoutedEventArgs e) =>
        ShowConfigWindow();

    private void OnLoginClick(object? sender, RoutedEventArgs e) =>
        ShowLoginScreen();

    private void RefreshUi()
    {
        if (ViewModel is null || _statsViewModel is null)
            return;

        ViewModel.Refresh();
        _statsViewModel.Apply(ViewModel.Snapshot);
        MapCanvas.Snapshot = ViewModel.Snapshot;
    }

    private void ShowConfigWindow()
    {
        if (Program.AppHost is null)
            return;

        _configWindow ??= new ConfigWindow(
            Program.AppHost.Services.GetRequiredService<ConfigTreeViewModel>());
        _configWindow.Show();
        _configWindow.Activate();
    }

    private void ShowLoginScreen()
    {
        if (Window.GetWindow(this) is ShellWindowView shell)
            shell.ShowLogin();
    }
}
