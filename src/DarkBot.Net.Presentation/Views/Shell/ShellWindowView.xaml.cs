using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using DarkBot.Net.Presentation.Logging;
using DarkBot.Net.Presentation.ViewModels.Shell;
using ReactiveUI;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Views.Shell;

/// <summary>
/// Единственное окно приложения. PageHost показывает Login или Main.
/// </summary>
public partial class ShellWindowView : ReactiveWindow<ShellWindowViewModel>
{
    private readonly GameShutdownCoordinator? _coordinator;
    private bool _shutdownStarted;

    /// <summary>Конструктор для XAML designer.</summary>
    public ShellWindowView()
    {
        InitializeComponent();
    }

    public ShellWindowView(ShellWindowViewModel viewModel, GameShutdownCoordinator coordinator)
    {
        _coordinator = coordinator;
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is null)
                return;

            ViewModel.WhenAnyValue(vm => vm.CurrentViewModel)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(NavigateToViewModel)
                .DisposeWith(disposables);
        });

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void NavigateToViewModel(object? viewModel)
    {
        Log.Information(
            "UI navigation: hosting view for {ViewModelType}",
            viewModel?.GetType().Name ?? "null");

        if (viewModel is null)
        {
            PageHost.Content = null;
            return;
        }

        try
        {
            var view = ViewLocator.Current.ResolveView(viewModel);
            if (view is null)
            {
                Log.Error(
                    "UI navigation: ViewLocator returned null for {ViewModelType}",
                    viewModel.GetType().Name);
                PageHost.Content = null;
                return;
            }

            if (view is IViewFor viewFor)
                viewFor.ViewModel = viewModel;

            PageHost.Content = view;
            Log.Information(
                "UI navigation: PageHost.Content set to {ViewType}",
                view.GetType().Name);

            PresentationUiDiagnostics.LogPageHostState(PageHost);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UI navigation: failed to host view for {ViewModelType}", viewModel.GetType().Name);
            PageHost.Content = null;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowBackgroundManager.UpdateBackground(this, ApplicationTheme.Dark, WindowBackdropType.Mica);

        if (ViewModel?.CurrentViewModel is not null)
            NavigateToViewModel(ViewModel.CurrentViewModel);

        PresentationUiDiagnostics.LogPageHostState(PageHost);

        Dispatcher.BeginInvoke(
            () => PresentationUiDiagnostics.LogPageHostState(PageHost),
            DispatcherPriority.Loaded);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_shutdownStarted || _coordinator is null)
            return;

        e.Cancel = true;
        _shutdownStarted = true;

        try
        {
            await _coordinator.StopGameClientAsync().ConfigureAwait(true);
        }
        finally
        {
            Closing -= OnClosing;
            Close();
        }
    }

    public void ShowLogin() => ViewModel?.ShowLogin();
}
