namespace DarkBot.Net.Core.Models.Game;

public sealed record GameConnectionStatusSnapshot(
    GameConnectionStatusKind Kind,
    string? FailureReason);
