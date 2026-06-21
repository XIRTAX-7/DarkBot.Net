using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using DarkBot.Net.Backpage;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Login;
using DarkBot.Net.Ui.Configuration;
using DarkBot.Net.Ui.Game;
using DarkBot.Net.Ui.Services;
using DarkBot.Net.Agent.Windows.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Ui.ViewModels;

public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly LoginService _loginService;
    private readonly BackpageService _backpage;
    private readonly StatsManager _stats;
    private readonly GameLaunchOrchestrator _gameLaunch;
    private readonly GameSessionStore _sessionStore;
    private readonly GameApiOptions _gameOptions;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _captchaToken = string.Empty;

    [ObservableProperty]
    private string _server = string.Empty;

    [ObservableProperty]
    private string _sid = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Sign in with username/password or paste a browser SID.";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    public LoginViewModel(
        LoginService loginService,
        BackpageService backpage,
        StatsManager stats,
        GameLaunchOrchestrator gameLaunch,
        GameSessionStore sessionStore,
        IOptions<GameApiOptions> gameOptions,
        IOptions<TestLoginOptions> testLoginOptions,
        ILogger<LoginViewModel> logger)
    {
        _loginService = loginService;
        _backpage = backpage;
        _stats = stats;
        _gameLaunch = gameLaunch;
        _sessionStore = sessionStore;
        _gameOptions = gameOptions.Value;
        _logger = logger;

        ApplyTestLoginDefaults(testLoginOptions.Value);
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

    public Window? OwnerWindow { get; set; }

    public event Action? LoginSucceeded;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task Login()
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

            _loginService.ApplySession(loginData, _backpage, _stats);

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
            LoginSucceeded?.Invoke();
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
        return await _loginService.LoginWithCredentialsAsync(
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
        return await _loginService.LoginWithSidAsync(Server.Trim(), Sid.Trim()).ConfigureAwait(true);
    }

    private bool CanLogin() => !IsBusy;
}
