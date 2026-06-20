using DarkBot.Net.Config;

namespace DarkBot.Net.Config.Tests;

public class StubConfigApiTests
{
    [Fact]
    public void ConfigRoot_HasBotSettings()
    {
        var api = new StubConfigApi();
        var children = api.GetChildren("BOT_SETTINGS");
        Assert.NotNull(children);
        Assert.Contains("MAP_DISPLAY", children);
    }

    [Fact]
    public void GetConfigValue_ReadsLeaf()
    {
        var api = new StubConfigApi();
        var value = api.GetConfigValue<bool>("BOT_SETTINGS.MAP_START_STOP");
        Assert.True(value);
    }

    [Fact]
    public void SetConfigProfile_AddsProfile()
    {
        var api = new StubConfigApi();
        api.SetConfigProfile("farm");
        Assert.Equal("farm", api.CurrentProfile);
        Assert.Contains("farm", api.ConfigProfiles);
    }
}
