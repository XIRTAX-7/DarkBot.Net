namespace DarkBot.Net.Core.Models.Game;

public sealed class GameLaunchParameters
{
    public required string InstanceUrl { get; init; }

    public required string Sid { get; init; }

    public required string PreloaderUrl { get; init; }

    public required IReadOnlyDictionary<string, string> FlashParams { get; init; }

    public int UserId { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}
