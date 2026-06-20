namespace DarkBot.Net.Plugins.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FeatureAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public bool EnabledByDefault { get; init; }
}
