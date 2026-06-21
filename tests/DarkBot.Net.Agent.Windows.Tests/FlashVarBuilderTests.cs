using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Infrastructure.Game.Tests;

public class FlashVarBuilderTests
{
    [Fact]
    public void BuildVarsString_applies_auto_start_and_2d_mode()
    {
        var vars = FlashVarBuilder.BuildVarsString(
            new Dictionary<string, string> { ["userID"] = "123" },
            new GameApiOptions { Use3D = false });

        Assert.Contains("autoStartEnabled=1", vars);
        Assert.Contains("display2d=2", vars);
        Assert.Contains("userID=123", vars);
    }
}
