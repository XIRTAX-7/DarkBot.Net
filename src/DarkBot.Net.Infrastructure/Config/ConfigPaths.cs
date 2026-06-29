using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Infrastructure.Config;

/// <summary>
/// Пути данных конфигурации в %LocalAppData%/DarkBot.Net.
/// Parity с Java: дефолтный профиль — config.json в корне; именованные — configs/*.json.
/// </summary>
public static class ConfigPaths
{
    public const string AppFolderName = "DarkBot.Net";

    /// <summary>Имя дефолтного user-профиля (как Java ConfigManager.DEFAULT).</summary>
    public const string DefaultUserProfileName = ConfigProfileNames.DefaultUser;

    /// <summary>Файл дефолтного профиля в корне data root.</summary>
    public const string DefaultUserProfileFileName = "config.json";

    /// <summary>Индекс сессии (отдельно от профиля; в Java нет).</summary>
    public const string SessionFileName = "session.json";

    public const string UserProfilesFolder = "configs";
    public const string AiProfilesFolder = "configs/ai";

    public static string GetDefaultDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);

    public static bool IsDefaultUserProfile(string profileName) =>
        profileName.Equals(DefaultUserProfileName, StringComparison.OrdinalIgnoreCase);

    public static string GetUserProfilePath(string dataRoot, string profileName) =>
        IsDefaultUserProfile(profileName)
            ? Path.Combine(dataRoot, DefaultUserProfileFileName)
            : Path.Combine(dataRoot, UserProfilesFolder, $"{profileName}.json");

    public static string GetAiProfilePath(string dataRoot, string profileName) =>
        Path.Combine(dataRoot, AiProfilesFolder, $"{profileName}.json");

    public static string GetSessionPath(string dataRoot) =>
        Path.Combine(dataRoot, SessionFileName);

    public static string GetBackupPath(string profilePath) =>
        Path.Combine(
            Path.GetDirectoryName(profilePath)!,
            $"{Path.GetFileNameWithoutExtension(profilePath)}_old.json");
}
