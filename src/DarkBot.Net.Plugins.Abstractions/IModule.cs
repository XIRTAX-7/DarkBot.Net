namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.extensions.Module — primary bot task controller.</summary>
public interface IModule
{
    void OnTickModule();
    void OnTickStopped() { }
    bool CanRefresh() => true;
    string? Status => null;
    string? StoppedStatus => null;
}
