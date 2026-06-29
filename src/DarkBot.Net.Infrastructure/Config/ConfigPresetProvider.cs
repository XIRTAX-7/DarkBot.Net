using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Infrastructure.Config;

/// <summary>Embedded JSON-пресеты — единственный источник стартовых значений (parity с Java config.json seed).</summary>
public static class ConfigPresetProvider
{
    private static readonly Assembly Assembly = typeof(ConfigPresetProvider).Assembly;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static BotProfileDocument LoadUserProfilePreset() =>
        LoadProfilePreset("config.json");

    public static BotProfileDocument LoadAiProfilePreset(string profileName) =>
        LoadProfilePreset($"ai/{profileName}.json");

    public static BotConfigIndex LoadSessionPreset() =>
        LoadPreset<BotConfigIndex>("session.json");

    public static void CopyPresetToFileIfMissing(string presetRelativePath, string targetPath)
    {
        if (File.Exists(targetPath))
            return;

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = ReadEmbeddedText(presetRelativePath);
        File.WriteAllText(targetPath, json);
    }

    public static BotProfileDocument LoadProfilePreset(string presetRelativePath)
    {
        var json = ReadEmbeddedText(presetRelativePath);
        return JsonSerializer.Deserialize<BotProfileDocument>(json, JsonOptions)
               ?? throw new InvalidDataException($"Embedded preset '{presetRelativePath}' is invalid.");
    }

    private static T LoadPreset<T>(string presetRelativePath)
    {
        var json = ReadEmbeddedText(presetRelativePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
               ?? throw new InvalidDataException($"Embedded preset '{presetRelativePath}' is invalid.");
    }

    private static string ReadEmbeddedText(string presetRelativePath)
    {
        var resourceName = $"{Assembly.GetName().Name}.Config.Presets.{presetRelativePath.Replace('/', '.')}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded preset not found: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
