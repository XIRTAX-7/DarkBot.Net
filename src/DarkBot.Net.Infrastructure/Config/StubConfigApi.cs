using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Infrastructure.Config;

/// <summary>Legacy stub for unit tests — production uses JsonConfigApi.</summary>
public sealed class StubConfigApi : IConfigApi
{
    private readonly ConfigSettingNode _root;
    private readonly List<string> _profiles = [ConfigProfileNames.DefaultUser];
    private string _currentProfile = ConfigProfileNames.DefaultUser;
    private readonly BotProfileDocument _document = ConfigPresetProvider.LoadUserProfilePreset();

    public StubConfigApi()
    {
        _root = ConfigTreeBuilder.Build(_document);
    }

    public IConfigSetting<object> ConfigRoot => _root;
    public BotProfileDocument CurrentDocument => _document;
    public ProfileOwner CurrentOwner => ProfileOwner.User;
    public IReadOnlyList<string> ConfigProfiles => _profiles;
    public string CurrentProfile => _currentProfile;

    public event EventHandler<ConfigProfileChangedEventArgs>? ProfileChanged;

    public void SetConfigProfile(string profile)
    {
        if (!_profiles.Contains(profile, StringComparer.OrdinalIgnoreCase))
            _profiles.Add(profile);

        _currentProfile = profile;
        ProfileChanged?.Invoke(this, new ConfigProfileChangedEventArgs(profile, ProfileOwner.User));
    }

    public void ReloadProfile()
    {
    }

    public void SetValue<T>(string path, T value, ConfigActor actor)
    {
    }

    public Task SaveAsync(ConfigActor actor, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public IConfigSetting<T>? GetConfig<T>(string path)
    {
        var node = FindNode(path);
        return node as IConfigSetting<T>;
    }

    public IConfigSetting<T> RequireConfig<T>(string path) =>
        GetConfig<T>(path) ?? throw new KeyNotFoundException($"Config path not found: {path}");

    public T? GetConfigValue<T>(string path)
    {
        var setting = FindNode(path);
        if (setting?.Value is T typed)
            return typed;

        return default;
    }

    public IReadOnlySet<string>? GetChildren(string path)
    {
        var node = FindNode(path);
        if (node is not IConfigSettingParent parent)
            return null;

        return parent.Children.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private IConfigSetting<object>? FindNode(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _root;

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IConfigSettingParent current = _root;

        foreach (var segment in segments)
        {
            if (!current.Children.TryGetValue(segment, out var next))
                return null;

            if (next is IConfigSettingParent parent)
                current = parent;
            else
                return segment == segments[^1] ? next : null;
        }

        return current;
    }
}
