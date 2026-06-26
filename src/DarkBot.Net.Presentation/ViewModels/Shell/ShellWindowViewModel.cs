using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Presentation.Resources;
using DarkBot.Net.Presentation.ViewModels.Login;
using DarkBot.Net.Presentation.ViewModels.Main;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;

namespace DarkBot.Net.Presentation.ViewModels.Shell;

/// <summary>
/// ViewModel единственного окна приложения.
/// Управляет <see cref="CurrentViewModel"/> — Login → Main.
/// </summary>
public sealed partial class ShellWindowViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _navigationSubscriptions = [];
    private readonly LoginViewModel _loginViewModel;
    private readonly MainWindowViewModel _mainViewModel;
    private readonly TitleBarDiagnosticsViewModel _titleBarDiagnostics;

    [Reactive] private string _applicationTitle = UiStrings.App_Title;

    public TitleBarDiagnosticsViewModel TitleBarDiagnostics => _titleBarDiagnostics;

    [Reactive(SetModifier = AccessModifier.Private)]
    private ViewModelBase _currentViewModel = null!;

    public ShellWindowViewModel(
        LoginViewModel loginViewModel,
        MainWindowViewModel mainViewModel,
        TitleBarDiagnosticsViewModel titleBarDiagnostics,
        IAppShellAppService appShell)
    {
        _loginViewModel = loginViewModel;
        _mainViewModel = mainViewModel;
        _titleBarDiagnostics = titleBarDiagnostics;

        loginViewModel.LoginSucceeded
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => NavigateToMain())
            .DisposeWith(_navigationSubscriptions);

        if (appShell.ShouldOpenMainScreen)
        {
            Log.Information("Saved credentials or session detected — opening main screen");
            CurrentViewModel = mainViewModel;
        }
        else
        {
            Log.Information("No saved credentials — showing login screen");
            CurrentViewModel = loginViewModel;
        }
    }

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public ShellWindowViewModel()
    {
        _loginViewModel = new LoginViewModel();
        _mainViewModel = new MainWindowViewModel();
        _titleBarDiagnostics = new TitleBarDiagnosticsViewModel();
        CurrentViewModel = _mainViewModel;
    }

    public void ShowLogin() => NavigateTo(_loginViewModel);

    public void ShowMain() => NavigateTo(_mainViewModel);

    private void NavigateToMain() => NavigateTo(_mainViewModel);

    private void NavigateTo(ViewModelBase viewModel) => CurrentViewModel = viewModel;

    public void Dispose() => _navigationSubscriptions.Dispose();
}
