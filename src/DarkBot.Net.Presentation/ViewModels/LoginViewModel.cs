using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Auth;
using DarkBot.Net.Infrastructure.Game;
using DarkBot.Net.Presentation.Configuration;
using DarkBot.Net.Presentation.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly ILoginAppService _loginApp;
    private readonly IGameLaunchAppService _gameLaunch;
    private readonly GameSessionStore _sessionStore;
    private readonly GameApiOptions _gameOptions;
    private readonly ILogger<LoginViewModel> _logger;
    private readonly IObservable<bool> _canLogin;
    private readonly Subject<Unit> _loginSucceeded = new();

    [Reactive] private int _selectedTabIndex;
    [Reactive] private string _username = string.Empty;
    [Reactive] private string _password = string.Empty;
    [Reactive] private string _captchaToken = string.Empty;
    [Reactive] private string _server = string.Empty;
    [Reactive] private string _sid = string.Empty;
    [Reactive] private string _statusMessage = "Sign in with username/password or paste a browser SID.";
    [Reactive] private bool _hasError;
    [Reactive] private bool _isBusy;

    public IObservable<Unit> LoginSucceeded => _loginSucceeded;

    public LoginViewModel(
        ILoginAppService loginApp,
        IGameLaunchAppService gameLaunch,
        GameSessionStore sessionStore,
        IOptions<GameApiOptions> gameOptions,
        IOptions<TestLoginOptions> testLoginOptions,
        ILogger<LoginViewModel> logger)
    {
        _loginApp = loginApp;
        _gameLaunch = gameLaunch;
        _sessionStore = sessionStore;
        _gameOptions = gameOptions.Value;
        _logger = logger;
        _canLogin = this.WhenAnyValue(x => x.IsBusy, busy => !busy);

        ApplyTestLoginDefaults(testLoginOptions.Value);
    }

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public LoginViewModel()
    {
        _loginApp = null!;
        _gameLaunch = null!;
        _sessionStore = null!;
        _gameOptions = new GameApiOptions();
        _logger = null!;
        _canLogin = Observable.Return(true);
        Username = "pilot";
        StatusMessage = "Design mode";
    }

    private void ApplyTestLoginDefaults(TestLoginOptions testLogin)
    {
        if (string.IsNullOrWhiteSpace(testLogin.Username) && string.IsNullOrWhiteSpace(testLogin.Password))
            return;

        Username = testLogin.Username.Trim();
        Password = testLogin.Password;
        SelectedTabIndex = 0;
        _logger.LogDebug("Login form pre-filled from appsettings.Local.json");
    }

    [ReactiveCommand(CanExecute = nameof(_canLogin))]
    private async Task LoginAsync()
    {
        HasError = false;
        IsBusy = true;

        var loginMode = SelectedTabIndex == 0 ? "credentials" : "sid";
        _logger.LogInformation("Login dialog submit ({Mode})", loginMode);

        try
        {
            LoginData loginData = SelectedTabIndex switch
            {
                0 => await LoginWithCredentialsAsync().ConfigureAwait(true),
                1 => await LoginWithSidAsync().ConfigureAwait(true),
                _ => throw new InvalidOperationException("Unknown login tab.")
            };

            _loginApp.ApplySession(loginData);

            var launchParameters = GameLaunchMapper.ToLaunchParameters(loginData);
            _sessionStore.Save(launchParameters);

            if (_gameOptions.BrowserApi == GameApiMode.BackpageOnly)
            {
                StatusMessage = "Session saved.";
            }
            else
            {
                StatusMessage = "Вход выполнен. Загрузка карты…";
                _gameLaunch.ScheduleLaunch(launchParameters);
            }

            _logger.LogInformation("Login dialog completed successfully ({Mode})", loginMode);
            _loginSucceeded.OnNext(Unit.Default);
        }
        catch (WrongCredentialsException ex)
        {
            HasError = true;
            StatusMessage = "Wrong username or password.";
            _logger.LogWarning(ex, "Login failed: wrong credentials ({Mode})", loginMode);
        }
        catch (CaptchaException ex)
        {
            HasError = true;
            StatusMessage = ex.Message;
            _logger.LogWarning(ex, "Login failed: captcha ({Mode})", loginMode);
        }
        catch (LoginException ex)
        {
            HasError = true;
            StatusMessage = ex.Message;
            _logger.LogWarning(ex, "Login failed ({Mode})", loginMode);
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = ex.Message;
            _logger.LogError(ex, "Login failed with unexpected error ({Mode})", loginMode);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<LoginData> LoginWithCredentialsAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            throw new LoginException("Username and password are required.");

        StatusMessage = "Logging in…";
        return await _loginApp.LoginWithCredentialsAsync(
            Username.Trim(),
            Password,
            string.IsNullOrWhiteSpace(CaptchaToken) ? null : CaptchaToken.Trim()).ConfigureAwait(true);
    }

    private async Task<LoginData> LoginWithSidAsync()
    {
        if (string.IsNullOrWhiteSpace(Server))
            throw new LoginException("Server prefix is required (for example ru1).");

        if (string.IsNullOrWhiteSpace(Sid))
            throw new LoginException("SID is required.");

        StatusMessage = "Loading spacemap…";
        return await _loginApp.LoginWithSidAsync(Server.Trim(), Sid.Trim()).ConfigureAwait(true);
    }
}
