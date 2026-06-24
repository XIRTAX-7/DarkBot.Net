using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Auth;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Повторная авторизация без перезапуска Unity: HTTP login → Frida refreshSession → WebView form.
/// </summary>
public sealed class UnitySessionRefresher(
    ILoginService loginService,
    UnityFridaSession fridaSession,
    GameSessionStore sessionStore,
    BackpageService backpage,
    ILogger<UnitySessionRefresher> logger)
{
    public async Task<bool> TryRefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        var launch = sessionStore.Current;
        if (launch is null
            || string.IsNullOrWhiteSpace(launch.Username)
            || string.IsNullOrWhiteSpace(launch.Password))
        {
            logger.LogWarning("Unity session refresh skipped — credentials not stored in GameSessionStore");
            return false;
        }

        if (!fridaSession.IsAttached)
        {
            logger.LogWarning("Unity session refresh skipped — Frida is not attached");
            return false;
        }

        try
        {
            var loginData = await loginService
                .LoginWithCredentialsAsync(launch.Username, launch.Password, null, cancellationToken)
                .ConfigureAwait(false);

            var instanceHost = loginData.InstanceHost
                ?? throw new LoginException("Login did not resolve instance host.");

            var session = new UnityWebGlSession(
                instanceHost,
                loginData.Sid!,
                string.Empty,
                launch.Username,
                launch.Password);

            backpage.SetSession(loginData.Sid!, loginData.UserId, loginData.InstanceUri!);
            await fridaSession.RefreshSessionAsync(session, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Unity session refreshed for userId={UserId}, sidSuffix={SidSuffix}",
                loginData.UserId,
                loginData.Sid!.Length >= 4 ? loginData.Sid[^4..] : loginData.Sid);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unity session refresh failed");
            return false;
        }
    }
}
