namespace DarkBot.Net.Core.Config;

/// <summary>Файловое хранилище профилей и индекса сессии.</summary>
public interface IConfigPersistence
{
    string DataRoot { get; }
    string UserProfilesDirectory { get; }
    string AiProfilesDirectory { get; }

    BotConfigIndex LoadIndex();
    void SaveIndex(BotConfigIndex index);

    IReadOnlyList<string> ListUserProfiles();
    IReadOnlyList<string> ListAiProfiles();

    BotProfileDocument LoadProfile(string profileName, ProfileOwner owner);
    void SaveProfile(string profileName, ProfileOwner owner, BotProfileDocument document);
    void DeleteProfile(string profileName, ProfileOwner owner);

    bool ProfileExists(string profileName, ProfileOwner owner);
    void EnsureInitialData();
}
