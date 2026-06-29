using System.Text.Json;
using System.Text.Json.Serialization;
using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Infrastructure.Config;

public sealed class JsonConfigPersistence : IConfigPersistence
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly object _fileGate = new();

    public JsonConfigPersistence(string? dataRoot = null)
    {
        DataRoot = dataRoot ?? ConfigPaths.GetDefaultDataRoot();
        UserProfilesDirectory = Path.Combine(DataRoot, ConfigPaths.UserProfilesFolder);
        AiProfilesDirectory = Path.Combine(DataRoot, ConfigPaths.AiProfilesFolder);
    }

    public string DataRoot { get; }
    public string UserProfilesDirectory { get; }
    public string AiProfilesDirectory { get; }

    public BotConfigIndex LoadIndex()
    {
        lock (_fileGate)
        {
            var indexPath = ConfigPaths.GetSessionPath(DataRoot);
            if (!File.Exists(indexPath))
                return ConfigPresetProvider.LoadSessionPreset();

            var json = File.ReadAllText(indexPath);
            return JsonSerializer.Deserialize<BotConfigIndex>(json, JsonOptions)
                   ?? ConfigPresetProvider.LoadSessionPreset();
        }
    }

    public void SaveIndex(BotConfigIndex index)
    {
        lock (_fileGate)
        {
            Directory.CreateDirectory(DataRoot);
            var indexPath = ConfigPaths.GetSessionPath(DataRoot);
            var tempPath = indexPath + ".tmp";
            var json = JsonSerializer.Serialize(index, JsonOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, indexPath, overwrite: true);
        }
    }

    public IReadOnlyList<string> ListUserProfiles()
    {
        lock (_fileGate)
        {
            var profiles = new List<string> { ConfigPaths.DefaultUserProfileName };
            profiles.AddRange(ListNamedUserProfilesUnlocked());
            return profiles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<string> ListAiProfiles() =>
        ListProfiles(AiProfilesDirectory);

    public BotProfileDocument LoadProfile(string profileName, ProfileOwner owner)
    {
        lock (_fileGate)
        {
            var path = GetProfilePath(profileName, owner);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Profile not found: {profileName}", path);

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BotProfileDocument>(json, JsonOptions)
                   ?? throw new InvalidDataException($"Profile '{profileName}' is empty or invalid.");
        }
    }

    public void SaveProfile(string profileName, ProfileOwner owner, BotProfileDocument document)
    {
        lock (_fileGate)
        {
            SaveProfileUnlocked(profileName, owner, document);
        }
    }

    public void DeleteProfile(string profileName, ProfileOwner owner)
    {
        lock (_fileGate)
        {
            var path = GetProfilePath(profileName, owner);
            if (File.Exists(path))
                File.Delete(path);

            var backupPath = ConfigPaths.GetBackupPath(path);
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }
    }

    public bool ProfileExists(string profileName, ProfileOwner owner)
    {
        lock (_fileGate)
            return File.Exists(GetProfilePath(profileName, owner));
    }

    public void EnsureInitialData()
    {
        lock (_fileGate)
        {
            Directory.CreateDirectory(DataRoot);
            Directory.CreateDirectory(UserProfilesDirectory);
            Directory.CreateDirectory(AiProfilesDirectory);

            ConfigPresetProvider.CopyPresetToFileIfMissing(
                "config.json",
                ConfigPaths.GetUserProfilePath(DataRoot, ConfigPaths.DefaultUserProfileName));

            ConfigPresetProvider.CopyPresetToFileIfMissing(
                "session.json",
                ConfigPaths.GetSessionPath(DataRoot));

            ConfigPresetProvider.CopyPresetToFileIfMissing(
                "ai/ai-pve.json",
                ConfigPaths.GetAiProfilePath(DataRoot, ConfigProfileNames.DefaultAiPve));

            ConfigPresetProvider.CopyPresetToFileIfMissing(
                "ai/ai-pvp.json",
                ConfigPaths.GetAiProfilePath(DataRoot, ConfigProfileNames.DefaultAiPvp));
        }
    }

    private bool ProfileExistsUnlocked(string profileName, ProfileOwner owner) =>
        File.Exists(GetProfilePath(profileName, owner));

    private void SaveProfileUnlocked(string profileName, ProfileOwner owner, BotProfileDocument document)
    {
        var path = GetProfilePath(profileName, owner);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        var backupPath = ConfigPaths.GetBackupPath(path);
        var json = JsonSerializer.Serialize(document, JsonOptions);

        if (File.Exists(tempPath))
            File.Delete(tempPath);

        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Move(path, backupPath);
        }

        File.Move(tempPath, path);
    }

    private string GetProfilePath(string profileName, ProfileOwner owner) =>
        owner switch
        {
            ProfileOwner.User => ConfigPaths.GetUserProfilePath(DataRoot, profileName),
            ProfileOwner.Ai => ConfigPaths.GetAiProfilePath(DataRoot, profileName),
            _ => throw new ArgumentOutOfRangeException(nameof(owner), owner, null)
        };

    private List<string> ListNamedUserProfilesUnlocked()
    {
        if (!Directory.Exists(UserProfilesDirectory))
            return [];

        return Directory
            .EnumerateFiles(UserProfilesDirectory, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !name.EndsWith("_old", StringComparison.OrdinalIgnoreCase))
            .Where(name => !name.Equals(ConfigProfileNames.DefaultUser, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ListProfiles(string directory)
    {
        if (!Directory.Exists(directory))
            return [];

        return Directory
            .EnumerateFiles(directory, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !name.EndsWith("_old", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
