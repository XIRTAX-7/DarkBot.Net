namespace DarkBot.Net.Application.BotEngine.Modules;

/// <summary>Пауза бота с опциональным таймером возобновления.</summary>
public sealed class DisconnectModule(long? pauseTimeMs, string reason)
{
    private readonly long? _pauseUntil = pauseTimeMs is null ? null : Environment.TickCount64 + pauseTimeMs.Value;

    public bool IsComplete => _pauseUntil is not null && Environment.TickCount64 >= _pauseUntil.Value;

    public string? Status => $"Disconnect: {reason}";

    public void OnTickModule()
    {
        // Phase 6: countdown + reconnect logic
    }
}
