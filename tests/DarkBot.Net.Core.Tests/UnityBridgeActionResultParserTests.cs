using DarkBot.Net.Infrastructure.Game.Bridge;

namespace DarkBot.Net.Infrastructure.Tests;

public class UnityBridgeActionResultParserTests
{
    [Theory]
    [InlineData("""{"ok":true,"api":"UnitHelper.SelectTarget"}""", true)]
    [InlineData("""{"ok":false,"error":"entity_pointer_not_found"}""", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParseOk_reads_ok_flag(string? json, bool expected)
    {
        Assert.Equal(expected, UnityBridgeActionResultParser.TryParseOk(json));
    }
}
