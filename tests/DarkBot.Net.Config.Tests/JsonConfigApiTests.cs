using DarkBot.Net.Core.Config;
using DarkBot.Net.Infrastructure.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Infrastructure.Config.Tests;

public class JsonConfigApiTests : IDisposable
{
    private readonly string _root;
    private readonly JsonConfigApi _api;

    public JsonConfigApiTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DarkBot.Net.Tests", Guid.NewGuid().ToString("N"));
        var persistence = new JsonConfigPersistence(_root);
        _api = new JsonConfigApi(persistence, new ConfigWritePolicy(), NullLogger<JsonConfigApi>.Instance);
    }

    public void Dispose()
    {
        _api.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void SetValue_UserActor_UpdatesDocument()
    {
        _api.SetValue("collect.radius", 600, ConfigActor.User);
        Assert.Equal(600, _api.GetConfigValue<int>("collect.radius"));
        Assert.Equal(600, _api.CurrentDocument.Collect.Radius);
    }

    [Fact]
    public void SetValue_AiActorOnUserProfile_Throws()
    {
        var act = () => _api.SetValue("collect.radius", 100, ConfigActor.Ai);
        Assert.Throws<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task SaveAsync_PersistsToDisk()
    {
        _api.SetValue("general.working_map", 42, ConfigActor.User);
        await _api.SaveAsync(ConfigActor.User);

        var persistence = new JsonConfigPersistence(_root);
        var loaded = persistence.LoadProfile(ConfigProfileNames.DefaultUser, ProfileOwner.User);
        Assert.Equal(42, loaded.General.WorkingMap);
    }
}
