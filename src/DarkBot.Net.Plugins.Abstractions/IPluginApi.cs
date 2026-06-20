namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.PluginAPI — DI facade for plugin constructors.</summary>
public interface IPluginApi
{
    T Require<T>() where T : class;
    T? Optional<T>() where T : class;
    T RequireInstance<T>() where T : class;
}
