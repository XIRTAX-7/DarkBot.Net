namespace DarkBot.Net.Core.Config;

/// <summary>
/// Уровень видимости настроек (аналог <c>Visibility.Level</c> в DarkBot API).
/// </summary>
public enum ConfigSettingVisibility
{
    Basic,
    Intermediate,
    Advanced,
    Developer,
}

/// <summary>Значения по умолчанию (как <c>CONFIG_LEVEL = Level.BASIC</c> в Java).</summary>
public static class ConfigSettingVisibilityDefaults
{
    public const ConfigSettingVisibility Level = ConfigSettingVisibility.Basic;
}

public static class ConfigSettingVisibilityExtensions
{
    public static bool IsVisibleAt(this ConfigSettingVisibility required, ConfigSettingVisibility threshold) =>
        required <= threshold;
}
