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
    public void ToFridaStatus_MapsHeroAndMapDimensions()
    {
        var agentStatus = new UnityBridgeAgentStatus
        {
            SchemaVersion = 1,
            Ready = true,
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
    public void ApplyAgentEvent_UpdatesHeroFromHeroPosEvent()
    {
        using var doc = JsonDocument.Parse("""{"type":"hero_pos","x":42,"y":84}""");
        UnityBridgeStatusMapper.ApplyAgentEvent(null, doc.RootElement, out var updated);

        Assert.NotNull(updated);
        Assert.True(updated!.Ready);
        Assert.Equal(42, updated.HeroX);
        Assert.Equal(84, updated.HeroY);
    }
}
