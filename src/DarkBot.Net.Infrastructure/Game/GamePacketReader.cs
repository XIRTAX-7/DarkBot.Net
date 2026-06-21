using System.Text.Json.Serialization;

namespace DarkBot.Net.Infrastructure.Game;

public sealed class GamePacketMessage
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

/// <summary>Receives game packets from Darkorbit-client WS :44569 (packet_dumper.py).</summary>
public sealed class GamePacketReader
{
    private static readonly string[] InvalidSessionTokens =
    [
        "invalid",
        "logout",
        "log_out",
        "disconnect",
        "session",
        "loginerror",
        "kicked"
    ];

    public DateTimeOffset? LastPacketAt { get; private set; }

    public event Action<GamePacketMessage>? PacketReceived;

    public event Action<GamePacketMessage>? InvalidSessionDetected;

    public void HandleMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        GamePacketMessage? message;
        try
        {
            message = System.Text.Json.JsonSerializer.Deserialize<GamePacketMessage>(json);
        }
        catch
        {
            return;
        }

        if (message is null)
            return;

        LastPacketAt = DateTimeOffset.UtcNow;
        PacketReceived?.Invoke(message);

        if (LooksLikeInvalidSession(message))
            InvalidSessionDetected?.Invoke(message);
    }

    private static bool LooksLikeInvalidSession(GamePacketMessage message)
    {
        var name = message.Name ?? string.Empty;
        foreach (var token in InvalidSessionTokens)
        {
            if (name.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
