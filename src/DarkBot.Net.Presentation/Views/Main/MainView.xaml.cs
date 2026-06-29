using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DarkBot.Net.Presentation.Controls.Main.MapCanvas;
using DarkBot.Net.Presentation.Ui.Main;
using DarkBot.Net.Presentation.ViewModels.Main;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;

namespace DarkBot.Net.Presentation.Views.Main;

public partial class MainView : ReactiveUserControl<MainWindowViewModel>
{
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
            this.OneWayBind(ViewModel, vm => vm.BotProfiles, v => v.BotProfileComboBox.ItemsSource)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedBotProfile, v => v.BotProfileComboBox.SelectedItem)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RestartClientCommand, v => v.RestartClientButton)
                .DisposeWith(disposables);

            MapCanvas.MapClicked += OnMapClicked;
            disposables.Add(Disposable.Create(() => MapCanvas.MapClicked -= OnMapClicked));

            if (Program.AppHost is null)
                return;

            var dashboard = Program.AppHost.Services.GetRequiredService<MainDashboardUiCoordinator>();
            dashboard.Attach(Dispatcher);
            disposables.Add(Disposable.Create(() => dashboard.Detach()));
        });
    }

    private async void OnMapClicked(object? sender, MapClickEventArgs e)
    {
        if (ViewModel is null)
            return;

        try
        {
            await ViewModel.MoveShipToMapLocationAsync(e).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Map click move failed");
        }
    }
}
