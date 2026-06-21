using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Auth;

namespace DarkBot.Net.Presentation.Game;

internal static class GameLaunchMapper
{
    public static GameLaunchParameters ToLaunchParameters(LoginData loginData)
    {
        if (loginData.InstanceUri is null ||
            string.IsNullOrWhiteSpace(loginData.Sid) ||
            loginData.PreloaderUrl is null ||
            loginData.FlashParams is null)
        {
            throw new LoginException("Login did not produce launch parameters.");
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
