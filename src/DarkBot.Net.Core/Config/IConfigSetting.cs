namespace DarkBot.Net.Core.Config;

public interface IConfigSetting<T>
{
    IConfigSettingParent? Parent { get; }
    string Key { get; }
    string Name { get; }
    string? Description { get; }
    Type ValueType { get; }
    T Value { get; set; }
}

public interface IConfigSettingParent : IConfigSetting<object>
{
    IReadOnlyDictionary<string, IConfigSetting<object>> Children { get; }
}
