using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Infrastructure.Config;

/// <summary>Phase 3 stub — minimal config tree until full Config port.</summary>
public sealed class StubConfigApi : IConfigApi
{
    private readonly ConfigSettingNode _root;
    private readonly List<string> _profiles = ["default"];
    private string _currentProfile = "default";

    public StubConfigApi()
    {
        _root = BuildDefaultTree();
    }

    public IConfigSetting<object> ConfigRoot => _root;
    public IReadOnlyList<string> ConfigProfiles => _profiles;
    public string CurrentProfile => _currentProfile;

    public void SetConfigProfile(string profile)
    {
        if (!_profiles.Contains(profile, StringComparer.OrdinalIgnoreCase))
            _profiles.Add(profile);

        _currentProfile = profile;
    }

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

    private static ConfigSettingNode BuildDefaultTree()
    {
        var root = new ConfigSettingNode("config", "Configuration");

        var botSettings = root.AddChild(new ConfigSettingNode("BOT_SETTINGS", "Bot settings"));
        botSettings.AddLeaf("ALWAYS_ON_TOP", "Always on top", false);
        botSettings.AddLeaf("MAP_START_STOP", "Click map to start/stop", true);

        var mapDisplay = botSettings.AddChild(new ConfigSettingNode("MAP_DISPLAY", "Map display"));
        mapDisplay.AddLeaf("SHOW_GRID", "Show grid", true);
        mapDisplay.AddLeaf("SHOW_HERO", "Show hero", true);

        var general = root.AddChild(new ConfigSettingNode("GENERAL", "General"));
        general.AddLeaf("WORKING_MAP", "Working map", "1-1");
        general.AddLeaf("SAFETY_WAIT", "Safety wait (ms)", 5000);

        return root;
    }
}
