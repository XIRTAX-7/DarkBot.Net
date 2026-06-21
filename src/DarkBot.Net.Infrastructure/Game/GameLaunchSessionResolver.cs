using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Восстанавливает параметры запуска из сохранённой сессии или backpage SID.
/// </summary>
public sealed class GameLaunchSessionResolver
{
    private readonly GameSessionStore _sessionStore;
    private readonly IBackpageApi _backpage;
    private readonly ILoginAppService _loginApp;
    private readonly GameApiOptions _options;

    public GameLaunchSessionResolver(
        GameSessionStore sessionStore,
        IBackpageApi backpage,
        ILoginAppService loginApp,
        IOptions<GameApiOptions> options)
    {
        _sessionStore = sessionStore;
        _backpage = backpage;
        _loginApp = loginApp;
        _options = options.Value;
    }

    public bool HasLaunchSession =>
        _options.BrowserApi != GameApiMode.BackpageOnly
        && (_sessionStore.HasSession
            || (!string.IsNullOrWhiteSpace(_backpage.Sid) && _backpage.InstanceUri is not null));

    public async Task<GameLaunchParameters?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_options.BrowserApi == GameApiMode.BackpageOnly)
            return null;

        if (_sessionStore.Current is not null)
            return _sessionStore.Current;

        if (string.IsNullOrWhiteSpace(_backpage.Sid) || _backpage.InstanceUri is null)
            return null;

        var host = _backpage.InstanceUri.Host;
        var server = host.Replace(".darkorbit.com", "", StringComparison.OrdinalIgnoreCase);
        var loginData = await _loginApp.LoginWithSidAsync(server, _backpage.Sid, cancellationToken)
            .ConfigureAwait(false);

        var launch = ToLaunchParameters(loginData);
        _sessionStore.Save(launch);
        return launch;
    }

    private static GameLaunchParameters ToLaunchParameters(LoginData loginData)
    {
        if (loginData.InstanceUri is null
            || string.IsNullOrWhiteSpace(loginData.Sid)
            || loginData.PreloaderUrl is null
            || loginData.FlashParams is null)
        {
            throw new InvalidOperationException("Login did not produce launch parameters.");
        }

        return new GameLaunchParameters
        {
            InstanceUrl = loginData.InstanceUri.ToString(),
            Sid = loginData.Sid,
            PreloaderUrl = loginData.PreloaderUrl,
            FlashParams = loginData.FlashParams,
            UserId = loginData.UserId,
            Username = loginData.Username,
            Password = loginData.Password
        };
    }
}
