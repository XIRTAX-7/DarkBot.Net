using System.Text.Json.Serialization;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Snapshot from darkDev GET /status — game pointers for BotInstaller / DarkMem.</summary>
public sealed class FridaBridgeStatus
{
    [JsonPropertyName("ready")]
    public bool Ready { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("mainApplicationAddress")]
    public string? MainApplicationAddress { get; init; }

    [JsonPropertyName("mainAddress")]
    public string? MainAddress { get; init; }

    [JsonPropertyName("screenManager")]
    public string? ScreenManager { get; init; }

    [JsonPropertyName("eventManager")]
    public string? EventManager { get; init; }

    [JsonPropertyName("heroStatic")]
    public string? HeroStatic { get; init; }

    [JsonPropertyName("connectionManager")]
    public string? ConnectionManager { get; init; }

    [JsonPropertyName("lastPacketActivityMs")]
    public long LastPacketActivityMs { get; init; }

    [JsonPropertyName("flashHookInstalled")]
    public bool FlashHookInstalled { get; init; }

    [JsonPropertyName("gotoMethodIndex")]
    public int GotoMethodIndex { get; init; }

    [JsonPropertyName("gotoMethodName")]
    public string? GotoMethodName { get; init; }

    public static long ParsePtr(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0")
            return 0;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToInt64(value, 16);

        return long.TryParse(value, out var parsed) ? parsed : 0;
    }
}
