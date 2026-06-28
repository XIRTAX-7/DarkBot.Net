using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows.Threading;
using DarkBot.Net.Presentation.Controls.Main;
using DarkBot.Net.Presentation.Controls.Main.MapCanvas;
using DarkBot.Net.Presentation.ViewModels.Main;
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

            this.Bind(ViewModel, vm => vm.BotRunning, v => v.ToggleBotButton.IsRunning)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ToggleBotCommand, v => v.ToggleBotButton.ActionButtonControl)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.OpenConfigCommand, v => v.OpenConfigButton)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RestartClientCommand, v => v.RestartClientButton)
                .DisposeWith(disposables);

            if (Program.AppHost is null)
                return;

            _statsViewModel = Program.AppHost.Services.GetRequiredService<StatsPanelViewModel>();
            StatsPanel.ViewModel = _statsViewModel;

            this.WhenAnyValue(v => v.StatsPanel!.ViewModel)
                .Where(vm => vm is not null)
                .Subscribe(vm => StatsPanel.DataContext = vm)
                .DisposeWith(disposables);

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
