using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Снимок из unity_bridge_agent.js getStatus().</summary>
public sealed class UnityBridgeAgentStatus
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("agentVersion")]
    public string? AgentVersion { get; init; }

    [JsonPropertyName("ready")]
    public bool Ready { get; init; }

    [JsonPropertyName("bootstrapHooksReady")]
    public bool BootstrapHooksReady { get; init; }

    [JsonPropertyName("clientUpdateStarted")]
    public bool ClientUpdateStarted { get; init; }

    [JsonPropertyName("clientUpdateComplete")]
    public bool ClientUpdateComplete { get; init; }

    [JsonPropertyName("clientUpdateCompleteAt")]
    public long ClientUpdateCompleteAt { get; init; }

    [JsonPropertyName("hangarDataReadyAt")]
    public long HangarDataReadyAt { get; init; }

    [JsonPropertyName("startButtonBoundAt")]
    public long StartButtonBoundAt { get; init; }

    [JsonPropertyName("sessionInjected")]
    public bool SessionInjected { get; init; }

    [JsonPropertyName("mapStartComplete")]
    public bool MapStartComplete { get; init; }

    [JsonPropertyName("mapStartRequested")]
    public bool MapStartRequested { get; init; }

    [JsonPropertyName("launchShowStarted")]
    public bool LaunchShowStarted { get; init; }

    [JsonPropertyName("launchShowStartAt")]
    public long LaunchShowStartAt { get; init; }

    [JsonPropertyName("startButtonPressed")]
    public bool StartButtonPressed { get; init; }

    [JsonPropertyName("webLoginOpened")]
    public bool WebLoginOpened { get; init; }

    [JsonPropertyName("getPostSeen")]
    public bool GetPostSeen { get; init; }

    [JsonPropertyName("movementHooksReady")]
    public bool MovementHooksReady { get; init; }

    [JsonPropertyName("pid")]
    public int Pid { get; init; }

    [JsonPropertyName("uptimeMs")]
    public long UptimeMs { get; init; }

    [JsonPropertyName("hookCount")]
    public int HookCount { get; init; }

    [JsonPropertyName("heroPos")]
    public UnityHeroPosition? HeroPos { get; init; }

    [JsonPropertyName("mapCenter")]
    public UnityMapCenter? MapCenter { get; init; }

    [JsonPropertyName("map")]
    public UnityMapSnapshot? Map { get; init; }
}

public sealed class UnityMapSnapshot
{
    [JsonPropertyName("mapId")]
    public int MapId { get; init; }

    [JsonPropertyName("mapName")]
    public string? MapName { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }
}

public sealed class UnityHeroPosition
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }

    [JsonPropertyName("serverY")]
    public int ServerY { get; init; }
}

public sealed class UnityMapCenter
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}

/// <summary>Маппинг Unity bridge status → FridaBridgeStatus для существующих probe/managers.</summary>
public static class UnityBridgeStatusMapper
{
    public const int DefaultMapWidth = 21000;
    public const int DefaultMapHeight = 13100;

    public static FridaBridgeStatus ToFridaStatus(UnityBridgeAgentStatus? status)
    {
        if (status is null)
        {
            return new FridaBridgeStatus
            {
                SchemaVersion = 1,
                Ready = false,
                MapWidth = DefaultMapWidth,
                MapHeight = DefaultMapHeight,
            };
        }

        var hero = status.HeroPos;
        var mapCenter = status.MapCenter;
        var map = status.Map;
        var width = map?.Width > 0
            ? map.Width
            : mapCenter?.X > 0 ? mapCenter.X * 2 : DefaultMapWidth;
        var height = map?.Height > 0
            ? map.Height
            : mapCenter?.Y > 0 ? mapCenter.Y * 2 : DefaultMapHeight;
        var mapId = map?.MapId ?? 0;
        var heroOnMap = status.MovementHooksReady && hero is not null;
        var heroY = hero switch
        {
            { ServerY: not 0 } positioned => positioned.ServerY,
            { Y: not 0 } positioned => positioned.Y,
            { X: > 1 } positioned => positioned.Y,
            _ => 0
        };

        return new FridaBridgeStatus
        {
            SchemaVersion = status.SchemaVersion,
            Ready = status.MovementHooksReady || (status.Ready && hero is not null),
            MainApplicationAddress = heroOnMap ? "0x1" : null,
            MainAddress = heroOnMap ? "0x1" : null,
            ScreenManager = heroOnMap ? "0x1" : null,
            ConnectionManager = heroOnMap ? "0x1" : null,
            HeroStatic = hero is not null ? "0x1" : "0x0",
            MapAddress = mapId > 0 ? "0x1" : null,
            MapId = mapId,
            MapWidth = width,
            MapHeight = height,
            HeroId = hero is not null ? 1 : 0,
            HeroX = hero?.X ?? 0,
            HeroY = heroY,
            HeroHp = heroOnMap ? 1 : 0,
            HeroMaxHp = heroOnMap ? 1 : 0,
            LastPacketActivityMs = Environment.TickCount64
        };
    }

    public static UnityBridgeAgentStatus? ParseStatusJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UnityBridgeAgentStatus>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void ApplyAgentEvent(FridaBridgeStatus? current, JsonElement payload, out FridaBridgeStatus? updated)
    {
        updated = current;
        if (!payload.TryGetProperty("type", out var typeProp))
            return;

        switch (typeProp.GetString())
        {
            case "ready":
                updated = Merge(current, ready: true);
                break;
            case "hero_pos":
                if (!payload.TryGetProperty("x", out var xProp)
                    || !payload.TryGetProperty("y", out var yProp))
                {
                    return;
                }

                updated = Merge(
                    current,
                    ready: true,
                    heroId: 1,
                    heroX: xProp.GetInt32(),
                    heroY: yProp.GetInt32(),
                    hasPointers: true);
                break;
            case "map_changed":
                if (!payload.TryGetProperty("mapId", out var mapIdProp))
                {
                    return;
                }

                var mapWidth = payload.TryGetProperty("width", out var widthProp)
                    ? widthProp.GetInt32()
                    : 0;
                var mapHeight = payload.TryGetProperty("height", out var heightProp)
                    ? heightProp.GetInt32()
                    : 0;

                updated = Merge(
                    current,
                    ready: true,
                    mapId: mapIdProp.GetInt32(),
                    mapWidth: mapWidth,
                    mapHeight: mapHeight,
                    hasPointers: true);
                break;
            case "ping":
                if (current is not null)
                    updated = Merge(current, touchActivity: true);
                break;
            case "client_update_started":
            case "client_update_complete":
            case "start_button_bound":
            case "session_injected":
            case "bootstrap_hooks_ready":
            case "movement_hooks_ready":
                if (current is not null)
                    updated = Merge(current, touchActivity: true);
                break;
        }
    }

    private static FridaBridgeStatus Merge(
        FridaBridgeStatus? current,
        bool ready = false,
        int heroId = 0,
        double heroX = 0,
        double heroY = 0,
        int mapId = 0,
        int mapWidth = 0,
        int mapHeight = 0,
        bool hasPointers = false,
        bool touchActivity = false)
    {
        var resolvedMapId = mapId > 0 ? mapId : current?.MapId ?? 0;
        var resolvedMapWidth = mapWidth > 0
            ? mapWidth
            : current?.MapWidth > 0 ? current.MapWidth : DefaultMapWidth;
        var resolvedMapHeight = mapHeight > 0
            ? mapHeight
            : current?.MapHeight > 0 ? current.MapHeight : DefaultMapHeight;

        return new FridaBridgeStatus
        {
            SchemaVersion = current?.SchemaVersion ?? 1,
            Ready = ready || current?.Ready == true,
            MainApplicationAddress = hasPointers ? "0x1" : current?.MainApplicationAddress,
            MainAddress = hasPointers ? "0x1" : current?.MainAddress,
            ScreenManager = hasPointers ? "0x1" : current?.ScreenManager,
            ConnectionManager = hasPointers ? "0x1" : current?.ConnectionManager,
            HeroStatic = hasPointers ? "0x1" : current?.HeroStatic,
            MapAddress = resolvedMapId > 0 ? "0x1" : current?.MapAddress,
            MapWidth = resolvedMapWidth,
            MapHeight = resolvedMapHeight,
            HeroId = heroId > 0 ? heroId : current?.HeroId ?? 0,
            HeroX = heroX != 0 ? heroX : current?.HeroX ?? 0,
            HeroY = heroY != 0 ? heroY : current?.HeroY ?? 0,
            LastPacketActivityMs = touchActivity || ready || heroId > 0 || mapId > 0
                ? Environment.TickCount64
                : current?.LastPacketActivityMs ?? 0,
            MapId = resolvedMapId,
            EntityCount = current?.EntityCount ?? 0,
            Entities = current?.Entities
        };
    }
}
