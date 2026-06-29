using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Tests.Fakes;

internal sealed class FakeConfigApi : IConfigApi
{
    public FakeConfigApi(BotProfileDocument? document = null) =>
        CurrentDocument = document ?? CreateDefaultDocument();

    public IConfigSetting<object> ConfigRoot { get; } = new FakeConfigRoot();

    public BotProfileDocument CurrentDocument { get; set; }

    public ProfileOwner CurrentOwner => ProfileOwner.User;

    public IReadOnlyList<string> ConfigProfiles { get; } = [ConfigProfileNames.DefaultUser];

    public string CurrentProfile => ConfigProfileNames.DefaultUser;

    public event EventHandler<ConfigProfileChangedEventArgs>? ProfileChanged;

    public void SetConfigProfile(string profile) { }

    public void ReloadProfile() { }

    public void SetValue<T>(string path, T value, ConfigActor actor) { }

    public Task SaveAsync(ConfigActor actor, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public IConfigSetting<T>? GetConfig<T>(string path) => null;

    public IConfigSetting<T> RequireConfig<T>(string path) =>
        throw new NotSupportedException("FakeConfigApi does not support RequireConfig.");

    public T? GetConfigValue<T>(string path)
    {
        if (path.Equals("general.current_module", StringComparison.OrdinalIgnoreCase)
            && typeof(T) == typeof(string))
            return (T)(object)CurrentDocument.General.CurrentModule;

        if (path.Equals("general.working_map", StringComparison.OrdinalIgnoreCase)
            && typeof(T) == typeof(int))
            return (T)(object)CurrentDocument.General.WorkingMap;

        if (path.Equals("collect.radius", StringComparison.OrdinalIgnoreCase)
            && typeof(T) == typeof(int))
            return (T)(object)CurrentDocument.Collect.Radius;

        return default;
    }

    public IReadOnlySet<string>? GetChildren(string path) => null;

    public static FakeConfigApi WithCollectorDefaults(int workingMap = 26, int collectRadius = 400) =>
        new(CreateDefaultDocument() with
        {
            General = CreateDefaultDocument().General with { WorkingMap = workingMap },
            Collect = CreateDefaultDocument().Collect with { Radius = collectRadius },
        });

    private static BotProfileDocument CreateDefaultDocument() =>
        new(
            new BotProfileMeta("Test", ProfileOwner.User),
            new BotProfileGeneral(ModuleIds.Collector, 26, 5000),
            new BotProfileCollect(
                400,
                false,
                false,
                true,
                new Dictionary<string, BoxInfoRecord>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BONUS_BOX"] = new(true, 1, 0),
                }));

    private sealed class FakeConfigRoot : IConfigSetting<object>
    {
        public IConfigSettingParent? Parent => null;
        public string Key => "config";
        public string Name => "Configuration";
        public string? Description => null;
        public Type ValueType => typeof(object);
        public object Value { get; set; } = new();
    }
}
