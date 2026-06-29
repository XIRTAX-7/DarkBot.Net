using DarkBot.Net.Infrastructure.Config;

namespace DarkBot.Net.Infrastructure.Config.Tests;

public class StubConfigApiTests
{
    [Fact]
    public void ConfigRoot_HasGeneralSection()
    {
        var api = new StubConfigApi();
        var children = api.GetChildren("general");
        Assert.NotNull(children);
        Assert.Contains("working_map", children);
    }

    [Fact]
    public void GetConfigValue_ReadsLeaf()
    {
        var api = new StubConfigApi();
        var value = api.GetConfigValue<int>("collect.radius");
        Assert.Equal(400, value);
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
