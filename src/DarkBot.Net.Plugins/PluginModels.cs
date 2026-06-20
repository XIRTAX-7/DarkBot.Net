using System.Reflection;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.Plugins;

public sealed class FeatureDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string PluginName { get; init; }
    public required string PluginPath { get; init; }
    public required Type FeatureType { get; init; }
    public bool EnabledByDefault { get; init; }
    public bool IsModule => typeof(IModule).IsAssignableFrom(FeatureType);
    public bool IsBehavior => typeof(IBehavior).IsAssignableFrom(FeatureType);
    public string? Instructions =>
        typeof(IInstructionProvider).IsAssignableFrom(FeatureType) ? "(see feature)" : null;
}

public sealed class LoadedFeature
{
    public required FeatureDescriptor Descriptor { get; init; }
    public bool Enabled { get; set; }
    public object? Instance { get; set; }
    public string? LoadError { get; set; }
}

public sealed class PluginDescriptor
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required Assembly Assembly { get; init; }
    public required PluginLoadContext Context { get; init; }
    public IReadOnlyList<FeatureDescriptor> Features { get; init; } = [];
}
