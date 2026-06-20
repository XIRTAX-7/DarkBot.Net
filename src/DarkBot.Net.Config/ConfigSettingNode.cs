using DarkBot.Net.Api.Config;

namespace DarkBot.Net.Config;

public sealed class ConfigSettingNode : IConfigSettingParent
{
    private readonly Dictionary<string, IConfigSetting<object>> _children = new(StringComparer.OrdinalIgnoreCase);

    public ConfigSettingNode(string key, string name, string? description = null, ConfigSettingNode? parent = null)
    {
        Key = key;
        Name = name;
        Description = description;
        Parent = parent;
    }

    public IConfigSettingParent? Parent { get; }
    public string Key { get; }
    public string Name { get; }
    public string? Description { get; }
    public Type ValueType => typeof(object);
    public object Value { get; set; } = null!;
    public IReadOnlyDictionary<string, IConfigSetting<object>> Children => _children;

    public ConfigSettingNode AddChild(ConfigSettingNode child)
    {
        _children[child.Key] = child;
        return child;
    }

    public ConfigSettingNode AddLeaf(string key, string name, object value, string? description = null)
    {
        var leaf = new ConfigLeafNode(key, name, value, description, this);
        _children[key] = leaf;
        return this;
    }
}

public sealed class ConfigLeafNode : IConfigSetting<object>
{
    public ConfigLeafNode(string key, string name, object value, string? description, ConfigSettingNode parent)
    {
        Key = key;
        Name = name;
        Description = description;
        Parent = parent;
        Value = value;
        ValueType = value.GetType();
    }

    public IConfigSettingParent? Parent { get; }
    public string Key { get; }
    public string Name { get; }
    public string? Description { get; }
    public Type ValueType { get; }
    public object Value { get; set; }
}
