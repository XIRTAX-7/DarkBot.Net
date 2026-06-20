namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.extensions.Behavior — side-effect logic alongside the active module.</summary>
public interface IBehavior
{
    void OnTickBehavior();
    void OnStoppedBehavior() { }
}
