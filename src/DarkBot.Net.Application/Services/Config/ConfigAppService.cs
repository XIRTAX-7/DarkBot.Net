using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Services.Config;

public sealed class ConfigAppService(
    IConfigApi configApi,
    IConfigSession session,
    IConfigPersistence persistence,
    IConfigWritePolicy writePolicy,
    StarManager starManager) : IConfigAppService
{
    private readonly IConfigApi _configApi = configApi;
    private readonly IConfigSession _session = session;
    private readonly IConfigPersistence _persistence = persistence;
    private readonly IConfigWritePolicy _writePolicy = writePolicy;
    private readonly StarManager _starManager = starManager;

    public ProfileOwner ActiveOwner => _session.ActiveOwner;
    public string ActiveProfile => _session.ActiveProfile;
    public bool IsEditable => ActiveOwner is ProfileOwner.User;

    public event EventHandler? ConfigChanged;

    public IReadOnlyList<ConfigProfileSummaryDto> ListUserProfiles() =>
        _persistence.ListUserProfiles()
            .Select(name => ToSummary(name, ProfileOwner.User))
            .ToList();

    public ConfigProfileSummaryDto? GetActiveProfile()
    {
        if (ActiveOwner is not ProfileOwner.User)
            return null;

        return ToSummary(ActiveProfile, ProfileOwner.User);
    }

    public AiProfileSummaryDto? GetAiProfileSummary()
    {
        if (!_persistence.ProfileExists(_session.LastAiProfile, ProfileOwner.Ai))
            return null;

        var document = _persistence.LoadProfile(_session.LastAiProfile, ProfileOwner.Ai);
        return new AiProfileSummaryDto(
            _session.LastAiProfile,
            document.Meta.DisplayName,
            document.General.WorkingMap,
            document.General.CurrentModule);
    }

    public void SwitchProfile(string profileName)
    {
        _writePolicy.EnsureCanWrite(profileName, ProfileOwner.User, ConfigActor.User);
        _session.SwitchToUserProfile(profileName);
        RaiseChanged();
    }

    public void CreateProfile(string profileName, string? displayName = null)
    {
        if (_persistence.ProfileExists(profileName, ProfileOwner.User))
            throw new InvalidOperationException($"Profile already exists: {profileName}");

        var template = _persistence.LoadProfile(ConfigProfileNames.DefaultUser, ProfileOwner.User);
        var document = template with
        {
            Meta = template.Meta with { DisplayName = displayName ?? profileName, Owner = ProfileOwner.User }
        };

        _persistence.SaveProfile(profileName, ProfileOwner.User, document);
        RaiseChanged();
    }

    public void RenameProfile(string profileName, string displayName)
    {
        EnsureUserProfile(profileName);
        var document = _persistence.LoadProfile(profileName, ProfileOwner.User);
        _persistence.SaveProfile(
            profileName,
            ProfileOwner.User,
            document with { Meta = document.Meta with { DisplayName = displayName } });

        if (ActiveProfile.Equals(profileName, StringComparison.OrdinalIgnoreCase))
            _configApi.ReloadProfile();

        RaiseChanged();
    }

    public void DeleteProfile(string profileName)
    {
        if (profileName.Equals(ConfigProfileNames.DefaultUser, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot delete the '{ConfigProfileNames.DefaultUser}' profile.");

        EnsureUserProfile(profileName);
        _persistence.DeleteProfile(profileName, ProfileOwner.User);

        if (ActiveProfile.Equals(profileName, StringComparison.OrdinalIgnoreCase))
            _session.SwitchToUserProfile(ConfigProfileNames.DefaultUser);

        RaiseChanged();
    }

    public void DuplicateProfile(string sourceProfileName, string newProfileName)
    {
        if (_persistence.ProfileExists(newProfileName, ProfileOwner.User))
            throw new InvalidOperationException($"Profile already exists: {newProfileName}");

        EnsureUserProfile(sourceProfileName);
        var source = _persistence.LoadProfile(sourceProfileName, ProfileOwner.User);
        var copy = source with
        {
            Meta = source.Meta with { DisplayName = newProfileName, Owner = ProfileOwner.User }
        };

        _persistence.SaveProfile(newProfileName, ProfileOwner.User, copy);
        RaiseChanged();
    }

    public ConfigCollectStateDto LoadCollectState()
    {
        var document = _configApi.CurrentDocument;
        var boxes = document.Collect.BoxInfos
            .OrderBy(pair => pair.Value.Priority)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ConfigBoxInfoRowDto(
                pair.Key,
                pair.Value.ShouldCollect,
                pair.Value.WaitTime,
                pair.Value.Priority))
            .ToList();

        return new ConfigCollectStateDto(
            document.Collect.StayAwayFromEnemies,
            document.Collect.AutoCloak,
            document.Collect.Radius,
            document.Collect.IgnoreContestedBoxes,
            boxes);
    }

    public void UpdateCollectSetting(string path, object value)
    {
        if (!IsEditable)
            throw new InvalidOperationException("Collect settings are read-only while AI controls the bot.");

        PushSetting(path, value);
        RaiseChanged();
    }

    public ConfigMainStateDto LoadMainState()
    {
        var document = _configApi.CurrentDocument;
        var maps = _starManager.EnumerateKnownMaps()
            .Select(pair => new MapOptionDto(pair.Id, pair.Name))
            .ToList();

        return new ConfigMainStateDto(
            document.General.CurrentModule,
            document.General.WorkingMap,
            document.General.SafetyWait,
            ModuleCatalog.Options,
            maps);
    }

    public void UpdateGeneralSetting(string path, object value)
    {
        if (!IsEditable)
            throw new InvalidOperationException("General settings are read-only while AI controls the bot.");

        PushSetting(path, value);
        RaiseChanged();
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _configApi.SaveAsync(ConfigActor.User, cancellationToken);

    private void PushSetting(string path, object value)
    {
        switch (value)
        {
            case bool boolValue:
                _configApi.SetValue(path, boolValue, ConfigActor.User);
                break;
            case int intValue:
                _configApi.SetValue(path, intValue, ConfigActor.User);
                break;
            case double doubleValue:
                _configApi.SetValue(path, (int)doubleValue, ConfigActor.User);
                break;
            case string stringValue:
                _configApi.SetValue(path, stringValue, ConfigActor.User);
                break;
            default:
                throw new ArgumentException($"Unsupported value type: {value.GetType().Name}", nameof(value));
        }
    }

    private ConfigProfileSummaryDto ToSummary(string name, ProfileOwner owner)
    {
        var document = _persistence.LoadProfile(name, owner);
        return new ConfigProfileSummaryDto(name, document.Meta.DisplayName);
    }

    private void EnsureUserProfile(string profileName)
    {
        if (!_persistence.ProfileExists(profileName, ProfileOwner.User))
            throw new FileNotFoundException($"User profile not found: {profileName}");
    }

    private void RaiseChanged() =>
        ConfigChanged?.Invoke(this, EventArgs.Empty);
}
