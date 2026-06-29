using DarkBot.Net.Core.Config;
using DarkBot.Net.Infrastructure.Config;

namespace DarkBot.Net.Infrastructure.Config.Tests;

public class JsonConfigPersistenceTests : IDisposable
{
    private readonly string _root;
    private readonly JsonConfigPersistence _persistence;

    public JsonConfigPersistenceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DarkBot.Net.Tests", Guid.NewGuid().ToString("N"));
        _persistence = new JsonConfigPersistence(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void EnsureInitialData_CreatesJavaParityLayout()
    {
        _persistence.EnsureInitialData();

        Assert.True(File.Exists(ConfigPaths.GetUserProfilePath(_root, ConfigProfileNames.DefaultUser)));
        Assert.True(File.Exists(ConfigPaths.GetSessionPath(_root)));
        Assert.True(_persistence.ProfileExists(ConfigProfileNames.DefaultUser, ProfileOwner.User));
        Assert.True(_persistence.ProfileExists(ConfigProfileNames.DefaultAiPve, ProfileOwner.Ai));
        Assert.Contains(ConfigProfileNames.DefaultUser, _persistence.ListUserProfiles());
        Assert.DoesNotContain(ConfigProfileNames.DefaultAiPve, _persistence.ListUserProfiles());
    }

    [Fact]
    public void SaveProfile_WritesBackupOnOverwrite()
    {
        _persistence.EnsureInitialData();
        var loaded = _persistence.LoadProfile(ConfigProfileNames.DefaultUser, ProfileOwner.User);
        var updated = loaded with
        {
            Collect = loaded.Collect with { Radius = 777 }
        };

        _persistence.SaveProfile(ConfigProfileNames.DefaultUser, ProfileOwner.User, updated);
        var reloaded = _persistence.LoadProfile(ConfigProfileNames.DefaultUser, ProfileOwner.User);

        Assert.Equal(777, reloaded.Collect.Radius);

        var backupPath = ConfigPaths.GetBackupPath(
            ConfigPaths.GetUserProfilePath(_root, ConfigProfileNames.DefaultUser));
        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public void NamedUserProfile_LivesUnderConfigsFolder()
    {
        _persistence.EnsureInitialData();
        var template = _persistence.LoadProfile(ConfigProfileNames.DefaultUser, ProfileOwner.User);
        _persistence.SaveProfile("farm", ProfileOwner.User, template);

        var path = ConfigPaths.GetUserProfilePath(_root, "farm");
        Assert.True(File.Exists(path));
        Assert.Contains("farm", _persistence.ListUserProfiles());
    }

    [Fact]
    public void SaveIndex_RoundTrips()
    {
        var index = new BotConfigIndex("farm", ConfigProfileNames.DefaultAiPve, ProfileOwner.User, "farm");
        _persistence.SaveIndex(index);

        var loaded = _persistence.LoadIndex();
        Assert.Equal("farm", loaded.LastUserProfile);
        Assert.Equal(ProfileOwner.User, loaded.ActiveOwner);
    }
}
