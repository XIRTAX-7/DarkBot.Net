using DarkBot.Net.Core.Modules;
using DarkBot.Net.Plugins;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.Core.Bot;

/// <summary>Ticks active module and enabled behaviors from the plugin registry.</summary>
public sealed class ModuleController
{
    private readonly IPluginRegistry _plugins;
    private IModule? _temporalModule;

    public ModuleController(IPluginRegistry plugins) => _plugins = plugins;

    public IModule? CurrentModule => _temporalModule ?? _plugins.ActiveModule;

    public IModule? NonTemporalModule =>
        _temporalModule is ITemporalModule temporal ? temporal.Back ?? _plugins.ActiveModule : _plugins.ActiveModule;

    public void SetTemporalModule(IModule? module) => _temporalModule = module;

    public void Tick(bool isRunning, bool heroValid)
    {
        if (_temporalModule is DisconnectModule disconnect && disconnect.IsComplete)
            _temporalModule = null;

        var module = CurrentModule;
        if (isRunning && heroValid)
        {
            module?.OnTickModule();
            foreach (var behavior in _plugins.EnabledBehaviors)
                behavior.OnTickBehavior();
        }
        else
        {
            module?.OnTickStopped();
            foreach (var behavior in _plugins.EnabledBehaviors)
                behavior.OnStoppedBehavior();
        }
    }
}
