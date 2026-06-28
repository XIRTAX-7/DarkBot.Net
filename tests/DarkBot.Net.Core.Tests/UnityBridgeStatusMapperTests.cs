using System.Text.Json;
using DarkBot.Net.Infrastructure.Game.Bridge;

namespace DarkBot.Net.Application.Tests;

public sealed class UnityBridgeStatusMapperTests
{
    [Fact]
    public void ParseStatusJson_ParsesHeroPosition()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "agentVersion": "test",
              "ready": true,
              "pid": 1234,
              "heroPos": { "x": 100, "y": -200, "serverY": 200 },
              "mapCenter": { "x": 10500, "y": 6550 }
            }
            """;

        var status = UnityBridgeStatusMapper.ParseStatusJson(json);
        Assert.NotNull(status);
        Assert.True(status!.Ready);
        Assert.Equal(100, status.HeroPos!.X);
        Assert.Equal(200, status.HeroPos.ServerY);
    }

    [Fact]
    public void ToFridaStatus_WhenHeroMissing_DoesNotThrow()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            Ready = true,
            HeroPos = null,
            MapCenter = new UnityMapCenter { X = 10500, Y = 6550 }
        };

        var frida = UnityBridgeStatusMapper.ToFridaStatus(agentStatus);

        Assert.False(frida.Ready);
        Assert.Equal(0, frida.HeroY);
    }

    [Fact]
    public void ToFridaStatus_WhenStatusNull_ReturnsNotReady()
    {
        var frida = UnityBridgeStatusMapper.ToFridaStatus(null);

        Assert.False(frida.Ready);
        Assert.Equal(UnityBridgeStatusMapper.DefaultMapWidth, frida.MapWidth);
    }

    [Fact]
    public void ToFridaStatus_MapsMapSnapshotFromAgent()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            Ready = true,
            MovementHooksReady = true,
            HeroPos = new UnityHeroPosition { X = 500, Y = -300, ServerY = 300 },
            Map = new UnityMapSnapshot
            {
                MapId = 16,
                MapName = "1-1",
                Width = 21000,
                Height = 13500
            }
        };

        var frida = UnityBridgeStatusMapper.ToFridaStatus(agentStatus);

        Assert.Equal(16, frida.MapId);
        Assert.Equal(21000, frida.MapWidth);
        Assert.Equal(13500, frida.MapHeight);
        Assert.Equal("0x1", frida.MapAddress);
    }

    [Fact]
    public void ApplyAgentEvent_UpdatesMapFromMapChangedEvent()
    {
        using var doc = JsonDocument.Parse("""{"type":"map_changed","mapId":9,"width":21000,"height":13500}""");
        UnityBridgeStatusMapper.ApplyAgentEvent(null, doc.RootElement, out var updated);

        Assert.NotNull(updated);
        Assert.Equal(9, updated!.MapId);
        Assert.Equal(21000, updated.MapWidth);
        Assert.Equal(13500, updated.MapHeight);
    }

    [Fact]
    public void ToFridaStatus_MapsHeroAndMapDimensions()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            Ready = true,
            MovementHooksReady = true,
            HeroPos = new UnityHeroPosition { X = 500, Y = -300, ServerY = 300 },
            MapCenter = new UnityMapCenter { X = 10500, Y = 6550 }
        };

        var frida = UnityBridgeStatusMapper.ToFridaStatus(agentStatus);

        Assert.True(frida.Ready);
        Assert.Equal(500, frida.HeroX);
        Assert.Equal(300, frida.HeroY);
        Assert.Equal(21000, frida.MapWidth);
        Assert.Equal(13100, frida.MapHeight);
        Assert.Equal("0x1", frida.ScreenManager);
    }

    [Fact]
    public void ToFridaStatus_MapsEntitiesFromAgent()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            MovementHooksReady = true,
            HeroPos = new UnityHeroPosition { X = 100, Y = -200, ServerY = 200 },
            Map = new UnityMapSnapshot { MapId = 16, Width = 21000, Height = 13500 },
            Entities =
            [
                new UnityBridgeEntity { Id = 42, X = 500, Y = 600, Kind = "npc", Label = "Streuner" },
                new UnityBridgeEntity { Id = 7, X = 1000, Y = 1100, Kind = "box", Fill = true }
            ],
            HeroHp = 12000,
            HeroMaxHp = 15000
        };

        var frida = UnityBridgeStatusMapper.ToFridaStatus(agentStatus);

        Assert.Equal(2, frida.EntityCount);
        Assert.NotNull(frida.Entities);
        Assert.Equal("npc", frida.Entities![0].Kind);
        Assert.Equal(12000, frida.HeroHp);
        Assert.Equal(15000, frida.HeroMaxHp);
    }

    [Fact]
    public void ApplyAgentEvent_UpdatesHeroFromHeroPosEvent()
    {
        using var doc = JsonDocument.Parse("""{"type":"hero_pos","x":42,"y":84}""");
        UnityBridgeStatusMapper.ApplyAgentEvent(null, doc.RootElement, out var updated);

        Assert.NotNull(updated);
        Assert.True(updated!.Ready);
        Assert.Equal(42, updated.HeroX);
        Assert.Equal(84, updated.HeroY);
    }

    [Fact]
    public void ApplyAgentEvent_UpdatesHeroHealthFromHeroHealthEvent()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "type":"hero_health",
              "hp":120000,
              "maxHp":180000,
              "shield":50000,
              "maxShield":90000,
              "nano":1000,
              "maxNano":2000,
              "shipType":"ship_venom",
              "playerName":"Pilot",
              "configId":2
            }
            """);
        UnityBridgeStatusMapper.ApplyAgentEvent(null, doc.RootElement, out var updated);

        Assert.NotNull(updated);
        Assert.Equal(120000, updated!.HeroHp);
        Assert.Equal(180000, updated.HeroMaxHp);
        Assert.Equal(50000, updated.HeroShield);
        Assert.Equal(90000, updated.HeroMaxShield);
        Assert.Equal(1000, updated.HeroNano);
        Assert.Equal(2000, updated.HeroMaxNano);
        Assert.Equal("ship_venom", updated.HeroShipType);
        Assert.Equal("Pilot", updated.HeroPlayerName);
        Assert.Equal(2, updated.HeroConfigId);
    }

    [Fact]
    public void ToFridaStatus_MapsHeroHealthFields()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            MovementHooksReady = true,
            HeroPos = new UnityHeroPosition { X = 100, Y = -200, ServerY = 200 },
            HeroHp = 12000,
            HeroMaxHp = 15000,
            HeroShield = 8000,
            HeroMaxShield = 10000,
            HeroNano = 500,
            HeroMaxNano = 1000,
            HeroShipType = "ship_goliath",
            HeroPlayerName = "Tester",
            HeroConfigId = 1
        };

        var frida = UnityBridgeStatusMapper.ToFridaStatus(agentStatus);

        Assert.Equal(8000, frida.HeroShield);
        Assert.Equal(10000, frida.HeroMaxShield);
        Assert.Equal(500, frida.HeroNano);
        Assert.Equal(1000, frida.HeroMaxNano);
        Assert.Equal("ship_goliath", frida.HeroShipType);
        Assert.Equal("Tester", frida.HeroPlayerName);
        Assert.Equal(1, frida.HeroConfigId);
    }

    [Fact]
    public void ToFridaStatus_MapsHeroHealthWithoutHeroPosition()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            Ready = true,
            MovementHooksReady = true,
            HeroUserId = 42,
            HeroPos = null,
            HeroHp = 12000,
            HeroMaxHp = 15000,
            HeroShield = 8000,
            HeroMaxShield = 10000,
            HeroShipType = "ship_goliath",
            HeroPlayerName = "Tester"
        };

        var frida = UnityBridgeStatusMapper.ToFridaStatus(agentStatus);

        Assert.True(frida.Ready);
        Assert.Equal(42, frida.HeroId);
        Assert.Equal("0x1", frida.HeroStatic);
        Assert.Equal(12000, frida.HeroHp);
        Assert.Equal(15000, frida.HeroMaxHp);
        Assert.Equal("Tester", frida.HeroPlayerName);
    }
}
