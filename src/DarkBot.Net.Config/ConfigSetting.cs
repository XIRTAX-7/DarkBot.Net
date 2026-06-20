using DarkBot.Net.Api.Config;

namespace DarkBot.Net.Config;

/// <summary>Simple in-memory config holder for plugin features.</summary>
public sealed class ConfigSetting<T>(T value) : IConfigSetting<T>
{
    public IConfigSettingParent? Parent => null;
    public string Key { get; init; } = typeof(T).Name;
    public string Name { get; init; } = typeof(T).Name;
    public string? Description => null;
    public Type ValueType => typeof(T);
    public T Value { get; set; } = value;
}
