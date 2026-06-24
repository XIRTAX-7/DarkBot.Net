using System.Text.Json;
using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Infrastructure.Auth.Tests;

public sealed class UnityWebGlLoginResolverTests
{
    [Fact]
    public void LooksLikeLoginNodeJson_RejectsShortPayload()
    {
        Assert.False(UnityWebGlLoginResolver.LooksLikeLoginNodeJson("{\"userID\":1}"));
    }

    [Fact]
    public void LooksLikeLoginNodeJson_RejectsHtmlWithUserIdKeyword()
    {
        var html = "<html><body>userID=123 and mapID=1 " + new string('x', 300) + "</body></html>";
        Assert.False(UnityWebGlLoginResolver.LooksLikeLoginNodeJson(html));
    }

    [Fact]
    public void LooksLikeLoginNodeJson_AcceptsTypicalLoginNodePayload()
    {
        var body = """
            {
              "userID": 173568365,
              "mapID": 1,
              "sessionID": "20cece488c5d9db2c390c27249318b23",
              "host": "https://ru1.darkorbit.com",
              "lang": "en",
              "browser": "unity",
              "platform": "standalone",
              "gameclientPath": "/",
              "basePath": "/",
              "itemXmlHash": "abc123def456789012345678901234567890",
              "resourcesXmlHash": "res789ghi012345678901234567890123456"
            }
            """;

        Assert.True(UnityWebGlLoginResolver.LooksLikeLoginNodeJson(body));
    }

    [Fact]
    public void TryUnwrapLoginDataNew_ExtractsInnerLoginNodeJson()
    {
        var inner = """
            {
              "userID": 173568365,
              "mapID": 1,
              "sessionID": "abc",
              "itemXmlHash": "abc123def456789012345678901234567890",
              "resourcesXmlHash": "res789ghi012345678901234567890123456"
            }
            """;

        var wrapped = JsonSerializer.Serialize(new { data = inner });
        var extracted = UnityWebGlLoginResolver.TryUnwrapLoginDataNew(wrapped);

        Assert.NotNull(extracted);
        Assert.True(UnityWebGlLoginResolver.LooksLikeLoginNodeJson(extracted!));
    }

    [Fact]
    public void TryExtractLoginJson_ParsesFlashEmbedParams()
    {
        var padding = new string('a', 200);
        var html = "<html><body>\n"
            + "flashembed(\"container\", {\"src\": \"https://ru1.darkorbit.com/preloader.swf\"}, "
            + "{\"userID\":\"4242\",\"mapID\":1,\"sessionID\":\"abc\",\"host\":\"https://ru1.darkorbit.com\","
            + "\"lang\":\"en\",\"itemXmlHash\":\"" + padding + "\"});\n"
            + "</body></html>";

        var json = UnityWebGlLoginResolver.TryExtractLoginJson(html);

        Assert.NotNull(json);
        Assert.True(UnityWebGlLoginResolver.LooksLikeLoginNodeJson(json));
        using var doc = JsonDocument.Parse(json!);
        Assert.Equal("4242", doc.RootElement.GetProperty("userID").GetString());
    }

    [Fact]
    public void TryCreateFromFlashParams_SerializesDictionary()
    {
        var flashParams = new Dictionary<string, string>
        {
            ["userID"] = "4242",
            ["mapID"] = "1",
            ["sessionID"] = "abc",
            ["itemXmlHash"] = new string('x', 200),
        };

        var session = UnityWebGlLoginResolver.TryCreateFromFlashParams(
            "ru1.darkorbit.com",
            "testsid",
            flashParams);

        Assert.NotNull(session);
        Assert.Equal("testsid", session!.Sid);
    }
}
