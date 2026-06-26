using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Presentation.Resources;
using DarkBot.Net.Presentation.ViewModels.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DarkBot.Net.Presentation.ViewModels.Login;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly ILoginAppService _login;
    private readonly ILogger<LoginViewModel> _logger;
    private readonly IObservable<bool> _canLogin;
    private readonly Subject<Unit> _loginSucceeded = new();

    [Reactive] private string _username = string.Empty;
    [Reactive] private string _password = string.Empty;
    [Reactive] private bool _rememberMe = true;
    [Reactive] private string _statusMessage = UiStrings.Login_Description;
    [Reactive] private bool _hasError;
    [Reactive] private bool _isBusy;

    public IObservable<Unit> LoginSucceeded => _loginSucceeded;

    public LoginViewModel(
        ILoginAppService login,
        IOptions<TestLoginOptions> testLoginOptions,
        ILogger<LoginViewModel> logger)
    {
        _login = login;
        _logger = logger;
        _canLogin = this.WhenAnyValue(
            x => x.IsBusy,
            x => x.Username,
            x => x.Password,
            (busy, username, password) =>
                !busy
                && !string.IsNullOrWhiteSpace(username)
                && !string.IsNullOrWhiteSpace(password));

        LoadSavedCredentials();
        ApplyTestLoginDefaults(testLoginOptions.Value);
        ScheduleAutoLoginFromTestCredentials(testLoginOptions.Value);
    }

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public LoginViewModel()
    {
        _login = null!;
        _logger = null!;
        _canLogin = Observable.Return(true);
        Username = "pilot";
        StatusMessage = UiStrings.Login_DesignMode;
    }

    private void LoadSavedCredentials()
    {
        if (!_login.TryLoadSavedCredentials(out var saved))
            return;

        Username = saved.Username;
        Password = saved.Password;
        RememberMe = true;
        _logger.LogDebug("Login form pre-filled from saved credentials");
    }

    private void ApplyTestLoginDefaults(TestLoginOptions testLogin)
    {
        if (string.IsNullOrWhiteSpace(testLogin.Username) && string.IsNullOrWhiteSpace(testLogin.Password))
            return;

        Username = testLogin.Username.Trim();
        Password = testLogin.Password;
        RememberMe = true;
        _logger.LogDebug("Login form pre-filled from appsettings.Local.json");
    }

    private void ScheduleAutoLoginFromTestCredentials(TestLoginOptions testLogin)
    {
        if (string.IsNullOrWhiteSpace(testLogin.Username) || string.IsNullOrWhiteSpace(testLogin.Password))
            return;

        _logger.LogInformation("TestLogin configured — scheduling automatic credential login");

        Observable.Timer(TimeSpan.FromMilliseconds(400), RxSchedulers.MainThreadScheduler)
            .SelectMany(_ => LoginCommand.Execute())
            .Subscribe(
                _ => { },
                ex => _logger.LogWarning(ex, "Automatic TestLogin failed"));
    }

    [ReactiveCommand(CanExecute = nameof(_canLogin))]
    private Task LoginAsync()
    {
        HasError = false;
        IsBusy = true;

        _logger.LogInformation("Login dialog submit (credentials)");

        try
        {
            _login.LoginWithCredentials(Username, Password, RememberMe);
            StatusMessage = UiStrings.Login_LaunchingGame;
            _logger.LogInformation("Login dialog completed — game launch scheduled");
            _loginSucceeded.OnNext(Unit.Default);
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = ex.Message;
            _logger.LogWarning(ex, "Login failed");
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }
}
