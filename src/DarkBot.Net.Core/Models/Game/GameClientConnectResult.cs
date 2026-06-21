namespace DarkBot.Net.Core.Models.Game;

public sealed class GameClientConnectResult
{
    public bool Success { get; init; }
    public int PepperPid { get; init; }
    public string? Error { get; init; }

    public static GameClientConnectResult Ok(int pid) => new() { Success = true, PepperPid = pid };

    public static GameClientConnectResult Fail(string error) => new() { Success = false, Error = error };
}
