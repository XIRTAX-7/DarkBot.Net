namespace DarkBot.Net.Application.DTOs.Responses.Bot;

/// <summary>Лёгкий read-model для title bar — без Frida/hero/map side effects.</summary>
public sealed record BotDiagnosticsSnapshot(
    double LastTickMs,
    double MemoryMb,
    int Ping,
    double LoopHz)
{
    public static BotDiagnosticsSnapshot Empty { get; } = new(0, 0, 0, 0);
}
