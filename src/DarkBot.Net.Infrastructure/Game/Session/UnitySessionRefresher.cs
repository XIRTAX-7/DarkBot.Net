using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Game.Bridge;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game.Session;

/// <summary>
/// Повторная авторизация без перезапуска Unity: Frida refreshSession → WebView form.
/// </summary>
public sealed class UnitySessionRefresher(
    UnityFridaSession fridaSession,
    GameSessionStore sessionStore,
    ILogger<UnitySessionRefresher> logger)
{
    public async Task<bool> TryRefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        var launch = sessionStore.Current;
        if (launch is null
            || string.IsNullOrWhiteSpace(launch.Username)
            || string.IsNullOrWhiteSpace(launch.Password))
        {
            logger.LogWarning("Unity session refresh skipped — launch credentials are missing");
            return false;
        }

        if (!fridaSession.IsAttached)
        {
            logger.LogWarning("Unity session refresh skipped — Frida session is not attached");
            return false;
        }

        try
        {
            var session = new UnityWebGlSession(launch.Username, launch.Password);
            await fridaSession.RefreshSessionAsync(session, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Unity session refresh RPC completed for user {Username}", launch.Username);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unity session refresh failed");
            return false;
        }
    }
}
