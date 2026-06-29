using DarkBot.Net.Application.Services.Config;
using DarkBot.Net.Core.Config;
using DarkBot.Net.Infrastructure.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Infrastructure.Config.Tests;

public class ConfigAppServiceTests : IDisposable
{
    private readonly string _root;
    private readonly JsonConfigPersistence _persistence;
    private readonly ConfigWritePolicy _writePolicy;
    private readonly JsonConfigApi _configApi;
    private readonly ConfigSession _session;
    private readonly ConfigAppService _appService;

    public ConfigAppServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DarkBot.Net.Tests", Guid.NewGuid().ToString("N"));
        _persistence = new JsonConfigPersistence(_root);
        _writePolicy = new ConfigWritePolicy();
        _configApi = new JsonConfigApi(_persistence, _writePolicy, NullLogger<JsonConfigApi>.Instance);
        _session = new ConfigSession(_persistence, _configApi);
        _session.Initialize();
        _appService = new ConfigAppService(_configApi, _session, _persistence, _writePolicy);
    }

    public void Dispose()
    {
        _configApi.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ListUserProfiles_IncludesConfigNotAiProfiles()
    {
        var profiles = _appService.ListUserProfiles();
        Assert.Contains(profiles, p => p.Name == ConfigProfileNames.DefaultUser);
        Assert.DoesNotContain(profiles, p => p.Name == ConfigProfileNames.DefaultAiPve);
    }

    [Fact]
    public void UpdateCollectSetting_PersistsRadius()
    {
        _appService.UpdateCollectSetting("collect.radius", 512);

        Assert.Equal(512, _configApi.GetConfigValue<int>("collect.radius"));
    }

    [Fact]
    public void CreateAndSwitchProfile_LoadsCollectState()
    {
        _appService.CreateProfile("farm");
        _appService.SwitchProfile("farm");
        _appService.UpdateCollectSetting("collect.radius", 333);

        var state = _appService.LoadCollectState();
        Assert.Equal(333, state.CollectRadius);
    }

    [Fact]
    public void UpdateCollectSetting_WhenAiControl_Throws()
    {
        _session.SwitchToAiControl(ConfigProfileNames.DefaultAiPve);

        var act = () => _appService.UpdateCollectSetting("collect.radius", 100);
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void GetAiProfileSummary_ReturnsReadOnlySnapshot()
    {
        var summary = _appService.GetAiProfileSummary();
        Assert.NotNull(summary);
        Assert.Equal(ConfigProfileNames.DefaultAiPve, summary!.Name);
        Assert.Equal(26, summary.WorkingMap);
    }

    [Fact]
    public void DeleteProfile_CannotDeleteConfig()
    {
        var act = () => _appService.DeleteProfile(ConfigProfileNames.DefaultUser);
        Assert.Throws<InvalidOperationException>(act);
    }
}
