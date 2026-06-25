using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Windows.Threading;
using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace DarkBot.Net.Presentation.Views.Main;

public partial class MainView : ReactiveUserControl<MainWindowViewModel>
{
    private StatsPanelViewModel? _statsViewModel;
    private DispatcherTimer? _refreshTimer;

    public MainView()
    {
        Log.Information("UI view: MainView created");
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is null)
                return;

            this.BindCommand(ViewModel, vm => vm.ToggleBotCommand, v => v.ToggleBotButton)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.OpenConfigCommand, v => v.OpenConfigButton)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.OpenLoginCommand, v => v.OpenLoginButton)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RestartClientCommand, v => v.RestartClientButton)
                .DisposeWith(disposables);

            if (Program.AppHost is null)
                return;

            _statsViewModel = Program.AppHost.Services.GetRequiredService<StatsPanelViewModel>();
            StatsPanel.ViewModel = _statsViewModel;

            MapCanvas.MapClicked += OnMapClicked;
            disposables.Add(Disposable.Create(() => MapCanvas.MapClicked -= OnMapClicked));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _refreshTimer.Tick += (_, _) => RefreshUi();
            _refreshTimer.Start();
            RefreshUi();

            disposables.Add(Disposable.Create(() => _refreshTimer?.Stop()));
        });
    }

    private void OnMapClicked(object? sender, MapClickEventArgs e) =>
        ViewModel?.MoveShipToMapLocation(e);

    private void RefreshUi()
    {
        if (ViewModel is null || _statsViewModel is null)
            return;

        ViewModel.Refresh();
        _statsViewModel.Apply(ViewModel.Snapshot);
        MapCanvas.Snapshot = ViewModel.Snapshot;
    }
}
