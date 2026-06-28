using System.Text.Json.Serialization;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Snapshot from darkDev GET /status — Frida AVM game state.</summary>
public sealed class FridaBridgeStatus
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

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

    [JsonPropertyName("mapAddress")]
    public string? MapAddress { get; init; }

    [JsonPropertyName("mapId")]
    public int MapId { get; init; }

    [JsonPropertyName("mapWidth")]
    public int MapWidth { get; init; }

    [JsonPropertyName("mapHeight")]
    public int MapHeight { get; init; }

    [JsonPropertyName("heroId")]
    public int HeroId { get; init; }

    [JsonPropertyName("heroX")]
    public double HeroX { get; init; }

    [JsonPropertyName("heroY")]
    public double HeroY { get; init; }

    [JsonPropertyName("heroHp")]
    public int HeroHp { get; init; }

    [JsonPropertyName("heroMaxHp")]
    public int HeroMaxHp { get; init; }

    [JsonPropertyName("heroShield")]
    public int HeroShield { get; init; }

    [JsonPropertyName("heroMaxShield")]
    public int HeroMaxShield { get; init; }

    [JsonPropertyName("heroNano")]
    public int HeroNano { get; init; }

    [JsonPropertyName("heroMaxNano")]
    public int HeroMaxNano { get; init; }

    [JsonPropertyName("heroShipType")]
    public string? HeroShipType { get; init; }

    [JsonPropertyName("heroPlayerName")]
    public string? HeroPlayerName { get; init; }

    [JsonPropertyName("heroConfigId")]
    public int HeroConfigId { get; init; }

    [JsonPropertyName("entityCount")]
    public int EntityCount { get; init; }

    [JsonPropertyName("entities")]
    public List<FridaBridgeEntity>? Entities { get; init; }

    [JsonPropertyName("zones")]
    public List<FridaBridgeZone>? Zones { get; init; }

    [JsonPropertyName("credits")]
    public long Credits { get; init; }

    [JsonPropertyName("uridium")]
    public long Uridium { get; init; }

    [JsonPropertyName("experience")]
    public long Experience { get; init; }

    [JsonPropertyName("honor")]
    public long Honor { get; init; }

    [JsonPropertyName("cargo")]
    public int Cargo { get; init; }

    [JsonPropertyName("maxCargo")]
    public int MaxCargo { get; init; }

    [JsonPropertyName("novaEnergy")]
    public int NovaEnergy { get; init; }

    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    public static long ParsePtr(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0")
            return 0;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToInt64(value, 16);

        return long.TryParse(value, out var parsed) ? parsed : 0;
    }
}

public sealed class FridaBridgeEntity
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("x")]
    public double X { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("fill")]
    public bool Fill { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("isEnemy")]
    public bool IsEnemy { get; init; }

    [JsonPropertyName("subKind")]
    public string? SubKind { get; init; }

    [JsonPropertyName("isGroupMember")]
    public bool IsGroupMember { get; init; }
}

public sealed class FridaBridgeZone
{
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("polygon")]
    public List<FridaBridgeZonePoint>? Polygon { get; init; }
}

public sealed class FridaBridgeZonePoint
{
    [JsonPropertyName("x")]
    public double X { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }
}
