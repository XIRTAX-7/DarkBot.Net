namespace DarkBot.Net.Core.Models.Game;

public sealed class GameLaunchParameters
{
    public required string Username { get; init; }

    public required string Password { get; init; }

    public static GameLaunchParameters FromCredentials(string username, string password) =>
        new()
        {
            Username = username,
            Password = password
        };
}
