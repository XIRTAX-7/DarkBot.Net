using DarkBot.Net.Login;

namespace DarkBot.Net.Ui.Game;

internal static class GameLaunchMapper
{
    public static Agent.Windows.Game.GameLaunchParameters ToLaunchParameters(LoginData loginData)
    {
        if (loginData.InstanceUri is null ||
            string.IsNullOrWhiteSpace(loginData.Sid) ||
            loginData.PreloaderUrl is null ||
            loginData.FlashParams is null)
        {
            throw new LoginException("Login did not produce launch parameters.");
        }

        return new Agent.Windows.Game.GameLaunchParameters
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
