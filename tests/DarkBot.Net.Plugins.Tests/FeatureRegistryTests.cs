using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Config;
using DarkBot.Net.Core;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;
using DarkBot.Net.DefaultPlugin;
using DarkBot.Net.Plugins;
using DarkBot.Net.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Plugins.Tests;

public class FeatureRegistryTests
{
    [Fact]
    public void LoadAll_discovers_default_plugin_features()
    {
        var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");
        Directory.CreateDirectory(pluginDir);
        var source = typeof(SampleModule).Assembly.Location;
        var target = Path.Combine(pluginDir, "DarkBot.Net.DefaultPlugin.dll");
        File.Copy(source, target, overwrite: true);

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.Configure<PluginOptions>(o => o.PluginsPath = pluginDir);
                services.AddDarkBotCore();
            })
            .Build();

        var registry = host.Services.GetRequiredService<IPluginRegistry>();
        registry.LoadAll();

        Assert.Equal(3, registry.Features.Count);
        Assert.Contains(registry.Features, f => f.Descriptor.Id.Contains(nameof(SampleModule), StringComparison.Ordinal));
        Assert.Contains(registry.Features, f => f.Descriptor.Id.Contains(nameof(AntiPush), StringComparison.Ordinal));
        Assert.Contains(registry.Features, f => f.Descriptor.Id.Contains(nameof(PalladiumModule), StringComparison.Ordinal));
    }

    [Fact]
    public void SampleModule_ticks_movement_when_configured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<BotAddressRegistry>();
        services.AddSingleton<IGameConnection, StubGameConnection>();
        services.AddSingleton<MovementApi>();
        services.AddSingleton<IMovementApi>(sp => sp.GetRequiredService<MovementApi>());
        services.AddSingleton<FeatureActivator>();
        var provider = services.BuildServiceProvider();

        var activator = provider.GetRequiredService<FeatureActivator>();
        var module = (SampleModule)activator.CreateInstance(typeof(SampleModule));
        module.SetConfig(new ConfigSetting<SampleModule.SampleConfig>(
            new SampleModule.SampleConfig { MoveShip = true }));

        module.OnTickModule();

        var movement = provider.GetRequiredService<MovementApi>();
        Assert.True(movement.IsMoving);
    }

    private sealed class StubGameConnection : IGameConnection
    {
        public GameApiMode Mode => GameApiMode.FridaClient;
        public GameConnectionPhase Phase => GameConnectionPhase.Connected;
        public bool IsLaunched => true;
        public bool IsValid => true;
        public string? LastFailureReason => null;
        public event Action<GameConnectionPhase>? PhaseChanged;
        public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0) { }
        public void SelectEntity(ReadOnlySpan<int> taggedArgs) { }
        public void UseItem(long screenManager, string itemId, int methodIndex, params long[] args) { }
        public void Refine(long refineUtilAddress, int oreId, int amount, int methodIndex = -1) { }
        public bool InvokeMethod(long objectPtr, int methodIndex, params long[] args) => false;
        public void Reload() { }
        public void HandleRefresh(bool useFakeDailyLogin = true) { }
        public long LastInternetReadTime() => 0;
        public void ClearCache(string pattern) { }
    }
}
