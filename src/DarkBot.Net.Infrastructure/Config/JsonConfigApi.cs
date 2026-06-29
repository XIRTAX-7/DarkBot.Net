using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Config;

public sealed class JsonConfigApi : IConfigApi, IDisposable
{
    private static readonly TimeSpan SaveDebounce = TimeSpan.FromSeconds(5);

    private readonly IConfigPersistence _persistence;
    private readonly IConfigWritePolicy _writePolicy;
    private readonly ILogger<JsonConfigApi> _logger;
    private readonly object _gate = new();
    private readonly Timer _saveTimer;

    private ConfigSettingNode _root = null!;
    private BotProfileDocument _document = null!;
    private string _currentProfile = ConfigProfileNames.DefaultUser;
    private ProfileOwner _currentOwner = ProfileOwner.User;
    private ConfigActor _pendingSaveActor = ConfigActor.User;
    private bool _saveScheduled;

    public JsonConfigApi(
        IConfigPersistence persistence,
        IConfigWritePolicy writePolicy,
        ILogger<JsonConfigApi> logger)
    {
        _persistence = persistence;
        _writePolicy = writePolicy;
        _logger = logger;
        _saveTimer = new Timer(OnSaveTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        LoadActiveProfileOrDefault();
    }

    public IConfigSetting<object> ConfigRoot => _root;
    public BotProfileDocument CurrentDocument => _document;
    public ProfileOwner CurrentOwner => _currentOwner;
    public string CurrentProfile => _currentProfile;

    public IReadOnlyList<string> ConfigProfiles =>
        _currentOwner switch
        {
            ProfileOwner.User => _persistence.ListUserProfiles(),
            ProfileOwner.Ai => _persistence.ListAiProfiles(),
            _ => []
        };

    public event EventHandler<ConfigProfileChangedEventArgs>? ProfileChanged;

    public void SetConfigProfile(string profile)
    {
        lock (_gate)
        {
            if (TryResolveProfile(profile, out var owner))
            {
                LoadProfile(profile, owner);
                return;
            }

            throw new FileNotFoundException($"Profile not found: {profile}");
        }
    }

    public void ReloadProfile()
    {
        lock (_gate)
            LoadProfile(_currentProfile, _currentOwner);
    }

    public void SetValue<T>(string path, T value, ConfigActor actor)
    {
        lock (_gate)
        {
            _writePolicy.EnsureCanWrite(_currentProfile, _currentOwner, actor);
            _document = ConfigDocumentMutator.Apply(_document, path, value);
            _root = ConfigTreeBuilder.Build(_document);
            ScheduleSave(actor);
        }
    }

    public async Task SaveAsync(ConfigActor actor, CancellationToken cancellationToken = default)
    {
        BotProfileDocument document;
        string profile;
        ProfileOwner owner;

        lock (_gate)
        {
            _writePolicy.EnsureCanWrite(_currentProfile, _currentOwner, actor);
            document = _document;
            profile = _currentProfile;
            owner = _currentOwner;
            _saveScheduled = false;
        }

        await Task.Run(() => _persistence.SaveProfile(profile, owner, document), cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Saved profile {Profile} (owner={Owner})", profile, owner);
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

    public void Dispose() => _saveTimer.Dispose();

    private void LoadActiveProfileOrDefault()
    {
        try
        {
            var index = _persistence.LoadIndex();
            if (_persistence.ProfileExists(index.ActiveProfile, index.ActiveOwner))
            {
                LoadProfile(index.ActiveProfile, index.ActiveOwner);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load config index; using in-memory defaults");
        }

        _document = ConfigPresetProvider.LoadUserProfilePreset();
        _currentProfile = ConfigProfileNames.DefaultUser;
        _currentOwner = ProfileOwner.User;
        _root = ConfigTreeBuilder.Build(_document);
    }

    private void LoadProfile(string profile, ProfileOwner owner)
    {
        _document = _persistence.LoadProfile(profile, owner);
        _currentProfile = profile;
        _currentOwner = owner;
        _root = ConfigTreeBuilder.Build(_document);
        ProfileChanged?.Invoke(this, new ConfigProfileChangedEventArgs(profile, owner));
    }

    private bool TryResolveProfile(string profile, out ProfileOwner owner)
    {
        if (_persistence.ProfileExists(profile, ProfileOwner.User))
        {
            owner = ProfileOwner.User;
            return true;
        }

        if (_persistence.ProfileExists(profile, ProfileOwner.Ai))
        {
            owner = ProfileOwner.Ai;
            return true;
        }

        owner = default;
        return false;
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

    private void ScheduleSave(ConfigActor actor)
    {
        _pendingSaveActor = actor;
        _saveScheduled = true;
        _saveTimer.Change(SaveDebounce, Timeout.InfiniteTimeSpan);
    }

    private async void OnSaveTimerElapsed(object? state)
    {
        if (!_saveScheduled)
            return;

        try
        {
            await SaveAsync(_pendingSaveActor).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debounced config save failed for profile {Profile}", _currentProfile);
        }
    }
}
