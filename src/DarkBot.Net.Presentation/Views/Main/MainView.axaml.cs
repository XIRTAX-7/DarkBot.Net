using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.ViewModels;
using DarkBot.Net.Presentation.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace DarkBot.Net.Presentation.Views.Main;

public partial class MainView : ReactiveUserControl<MainWindowViewModel>
{
    private StatsPanelViewModel? _statsViewModel;
    private StatsPanelControl? _statsPanel;
    private MapCanvasControl? _mapCanvas;
    private Button? _configButton;
    private Button? _loginButton;
    private DispatcherTimer? _refreshTimer;
    private ConfigWindow? _configWindow;

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);

        this.WhenActivated(disposables =>
        {
            if (Program.AppHost is null || ViewModel is null)
                return;

            _statsPanel = this.FindControl<StatsPanelControl>("StatsPanel");
            _mapCanvas = this.FindControl<MapCanvasControl>("MapCanvas");
            _configButton = this.FindControl<Button>("ConfigButton");
            _loginButton = this.FindControl<Button>("LoginButton");

            if (_statsPanel is null || _mapCanvas is null || _configButton is null || _loginButton is null)
                return;

            _statsViewModel = Program.AppHost.Services.GetRequiredService<StatsPanelViewModel>();
            _statsPanel.ViewModel = _statsViewModel;

            _mapCanvas.MapClicked += OnMapClicked;
            disposables.Add(Disposable.Create(() => _mapCanvas.MapClicked -= OnMapClicked));

            _configButton.Click += OnConfigClick;
            _loginButton.Click += OnLoginClick;
            disposables.Add(Disposable.Create(() =>
            {
                _configButton.Click -= OnConfigClick;
                _loginButton.Click -= OnLoginClick;
            }));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _refreshTimer.Tick += (_, _) => RefreshUi();
            _refreshTimer.Start();
            RefreshUi();

            disposables.Add(Disposable.Create(() => _refreshTimer?.Stop()));
        });
    }

    private void OnMapClicked(object? sender, EventArgs e) =>
        ViewModel?.ToggleBotFromMapCommand.Execute(Unit.Default).Subscribe();

    private void OnConfigClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ShowConfigWindow();

    private void OnLoginClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ShowLoginScreen();

    private void RefreshUi()
    {
        if (ViewModel is null || _statsViewModel is null || _mapCanvas is null || _statsPanel is null)
            return;

        ViewModel.Refresh();
        _statsViewModel.Apply(ViewModel.Snapshot);
        _mapCanvas.Snapshot = ViewModel.Snapshot;
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
        if (TopLevel.GetTopLevel(this) is ShellWindowView { ViewModel: { } shell })
            shell.ShowLogin();
    }
}
