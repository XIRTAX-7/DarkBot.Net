using DarkBot.Net.Core.Config;
using DarkBot.Net.Infrastructure.Config;

namespace DarkBot.Net.Infrastructure.Config.Tests;

public class ConfigPresetProviderTests
{
    [Fact]
    public void LoadUserProfilePreset_HasCollectRadius()
    {
        var document = ConfigPresetProvider.LoadUserProfilePreset();
        Assert.Equal(400, document.Collect.Radius);
        Assert.Equal(ProfileOwner.User, document.Meta.Owner);
    }

    [Fact]
    public void LoadSessionPreset_UsesConfigProfileName()
    {
        var index = ConfigPresetProvider.LoadSessionPreset();
        Assert.Equal(ConfigProfileNames.DefaultUser, index.ActiveProfile);
        Assert.Equal(ConfigProfileNames.DefaultUser, index.LastUserProfile);
    }
}
