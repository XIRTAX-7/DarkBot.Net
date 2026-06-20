using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.Core.Modules;

/// <summary>Port of DisconnectModule — pauses the bot with optional timed resume.</summary>
public sealed class DisconnectModule(long? pauseTimeMs, string reason) : ITemporalModule
{
    private readonly long? _pauseUntil = pauseTimeMs is null ? null : Environment.TickCount64 + pauseTimeMs.Value;

    public IModule? Back { get; private set; }
    public bool IsComplete => _pauseUntil is not null && Environment.TickCount64 >= _pauseUntil.Value;
    public string? Status => $"Disconnect: {reason}";

    public void OnTickModule() { }
    public void OnTickStopped() { }
    public bool CanRefresh() => false;
}
