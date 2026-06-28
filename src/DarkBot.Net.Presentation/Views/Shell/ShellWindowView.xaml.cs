using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows.Threading;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Presentation.Diagnostics;
using DarkBot.Net.Presentation.Ui.Shell;
using DarkBot.Net.Presentation.ViewModels.Shell;
using ReactiveUI;
using Serilog;
using Wpf.Ui.Appearance;

namespace DarkBot.Net.Presentation.Views.Shell;

/// <summary>
/// Единственное окно приложения. PageHost показывает Login или Main.
/// </summary>
public partial class ShellWindowView : ShellWindowViewBase
{
    private readonly IGameShutdownAppService? _gameShutdown;
    private readonly TitleBarDiagnosticsUiCoordinator? _titleBarDiagnostics;
    private bool _shutdownStarted;

    /// <summary>Конструктор для XAML designer.</summary>
    public ShellWindowView()
    {
        InitializeComponent();
    }

    public ShellWindowView(
        ShellWindowViewModel viewModel,
        IGameShutdownAppService gameShutdown,
        TitleBarDiagnosticsUiCoordinator titleBarDiagnostics)
    {
        _gameShutdown = gameShutdown;
        _titleBarDiagnostics = titleBarDiagnostics;
        ViewModel = viewModel;
        DataContext = viewModel;

        SystemThemeWatcher.Watch(this);
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
        Closed += OnClosed;
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

            if (view is System.Windows.FrameworkElement element)
                element.DataContext = viewModel;

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

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _titleBarDiagnostics?.Attach(Dispatcher);

        if (ViewModel?.CurrentViewModel is not null)
            NavigateToViewModel(ViewModel.CurrentViewModel);

        PresentationUiDiagnostics.LogPageHostState(PageHost);

        Dispatcher.BeginInvoke(
            () => PresentationUiDiagnostics.LogPageHostState(PageHost),
            DispatcherPriority.Loaded);
    }

    private void OnClosed(object? sender, EventArgs e) =>
        _titleBarDiagnostics?.Detach();

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_shutdownStarted || _gameShutdown is null)
            return;

        e.Cancel = true;
        _shutdownStarted = true;

        try
        {
            await _gameShutdown.StopGameClientAsync().ConfigureAwait(true);
        }
        finally
        {
            Closing -= OnClosing;
            Close();
        }
    }

    public void ShowLogin() => ViewModel?.ShowLogin();
}
