namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.extensions.TemporalModule — short-lived module override.</summary>
public interface ITemporalModule : IModule
{
    IModule? Back { get; }
}
