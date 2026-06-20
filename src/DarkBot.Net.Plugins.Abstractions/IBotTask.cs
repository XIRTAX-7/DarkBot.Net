namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.extensions.Task — background/backpage work.</summary>
public interface IBotTask
{
    void OnTickTask();
    void OnBackgroundTick() { }
}
