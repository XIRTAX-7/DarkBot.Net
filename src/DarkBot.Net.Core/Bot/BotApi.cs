using DarkBot.Net.Core.Bot;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.Core.Bot;

public sealed class BotApi : IBotApi
{
    private readonly IBotController _controller;
    private readonly ModuleController _modules;
    private readonly BotLoopService _loop;

    public BotApi(IBotController controller, ModuleController modules, BotLoopService loop)
    {
        _controller = controller;
        _modules = modules;
        _loop = loop;
    }

    public double TickTime => _loop.LastTickMs;
    public bool IsRunning => _controller.IsRunning;
    public IModule? Module => _modules.CurrentModule;
    public IModule? NonTemporalModule => _modules.NonTemporalModule;

    public void SetRunning(bool running)
    {
        if (running)
            _controller.Start();
        else
            _controller.Pause();
    }

    public T? SetModule<T>(T? module) where T : class, IModule
    {
        _modules.SetTemporalModule(module);
        return module;
    }
}
