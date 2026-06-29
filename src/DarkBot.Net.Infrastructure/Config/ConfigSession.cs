using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Infrastructure.Config;

public sealed class ConfigSession(
    IConfigPersistence persistence,
    IConfigApi configApi) : IConfigSession
{
    private readonly IConfigPersistence _persistence = persistence;
    private readonly IConfigApi _configApi = configApi;

    public ProfileOwner ActiveOwner { get; private set; }
    public string ActiveProfile { get; private set; } = string.Empty;
    public string LastUserProfile { get; private set; } = ConfigProfileNames.DefaultUser;
    public string LastAiProfile { get; private set; } = "ai-pve";

    public event EventHandler? SessionChanged;

    public void Initialize()
    {
        _persistence.EnsureInitialData();
        var index = _persistence.LoadIndex();

        LastUserProfile = index.LastUserProfile;
        LastAiProfile = index.LastAiProfile;
        ActiveOwner = index.ActiveOwner;
        ActiveProfile = index.ActiveProfile;

        _configApi.SetConfigProfile(ActiveProfile);
    }

    public void SwitchToUserProfile(string profileName)
    {
        if (!_persistence.ProfileExists(profileName, ProfileOwner.User))
            throw new FileNotFoundException($"User profile not found: {profileName}");

        LastUserProfile = profileName;
        ActiveOwner = ProfileOwner.User;
        ActiveProfile = profileName;
        PersistIndex();
        _configApi.SetConfigProfile(profileName);
        RaiseChanged();
    }

    public void SwitchToAiProfile(string profileName)
    {
        if (!_persistence.ProfileExists(profileName, ProfileOwner.Ai))
            throw new FileNotFoundException($"AI profile not found: {profileName}");

        LastAiProfile = profileName;
        ActiveOwner = ProfileOwner.Ai;
        ActiveProfile = profileName;
        PersistIndex();
        _configApi.SetConfigProfile(profileName);
        RaiseChanged();
    }

    public void SwitchToAiControl(string aiProfileName) =>
        SwitchToAiProfile(aiProfileName);

    public void RestoreUserControl() =>
        SwitchToUserProfile(LastUserProfile);

    private void PersistIndex()
    {
        _persistence.SaveIndex(new BotConfigIndex(
            LastUserProfile,
            LastAiProfile,
            ActiveOwner,
            ActiveProfile));
    }

    private void RaiseChanged() =>
        SessionChanged?.Invoke(this, EventArgs.Empty);
}
