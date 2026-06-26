using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Application.Services.Auth;

public sealed class LoginAppService(
    ICredentialStore credentialStore,
    IGameLaunchAppService gameLaunch,
    IGameSessionStore sessionStore) : ILoginAppService
{
    public bool HasSavedCredentials => credentialStore.HasSaved;

    public bool TryLoadSavedCredentials(out SavedCredentials credentials) =>
        credentialStore.TryLoad(out credentials);

    public void LoginWithCredentials(string username, string password, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Username and password are required.");

        var normalizedUsername = username.Trim();

        if (rememberMe)
            credentialStore.Save(new SavedCredentials(normalizedUsername, password));
        else
            credentialStore.Clear();

        var launchParameters = GameLaunchParameters.FromCredentials(normalizedUsername, password);
        sessionStore.Save(launchParameters);
        gameLaunch.ScheduleLaunch(launchParameters);
    }
}
