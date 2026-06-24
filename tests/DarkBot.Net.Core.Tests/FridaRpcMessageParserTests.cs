using System.Text.Json;
using DarkBot.Net.Infrastructure.Game.Bridge;

namespace DarkBot.Net.Application.Tests;

public sealed class FridaRpcMessageParserTests
{
    [Fact]
    public void TryParseSendPayload_ParsesRpcOkResponse()
    {
        const string message = """{"type":"send","payload":["frida:rpc",7,"ok","result-json"]}""";

        var parsed = FridaRpcClient.TryParseSendPayload(message, out var payload);

        Assert.True(parsed);
        Assert.Equal(JsonValueKind.Array, payload.ValueKind);
        Assert.Equal("frida:rpc", payload[0].GetString());
        Assert.Equal(7, payload[1].GetInt32());
        Assert.Equal("ok", payload[2].GetString());
        Assert.Equal("result-json", payload[3].GetString());
    }

    [Fact]
    public void TryParseSendPayload_ParsesAgentEventObject()
    {
        const string message = """{"type":"send","payload":{"type":"ready","schemaVersion":1}}""";

        var parsed = FridaRpcClient.TryParseSendPayload(message, out var payload);

        Assert.True(parsed);
        Assert.Equal(JsonValueKind.Object, payload.ValueKind);
        Assert.Equal("ready", payload.GetProperty("type").GetString());
    }
}
