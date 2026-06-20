namespace DarkBot.Net.Agent.Windows.Game;

public enum KekkaPlayerWindowLoopState
{
    Idle = 0,
    Running = 1,
    Exited = 2,
    Failed = 3,
}

public sealed class KekkaPlayerWindowStatus
{
    public required KekkaPlayerWindowLoopState State { get; init; }

    public long DurationMs { get; init; }

    public string Detail { get; init; } = string.Empty;
}
