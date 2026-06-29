using System.Text.Json;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Парсит JSON-ответ action RPC агента ({ "ok": true/false, ... }).</summary>
public static class UnityBridgeActionResultParser
{
    public static bool TryParseOk(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.ValueKind == JsonValueKind.True)
                return true;
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
