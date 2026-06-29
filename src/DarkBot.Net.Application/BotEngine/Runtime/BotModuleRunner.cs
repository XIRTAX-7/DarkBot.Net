using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Application.BotEngine.Safety;
using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.BotEngine.Runtime;

/// <summary>Встроенные модули бота (internal C# classes; plugins — Phase 8, см. docs/adr/001-internal-modules.md).</summary>
public sealed class BotModuleRunner
{
    private readonly ModuleContext _context;
    private readonly IConfigApi _config;
    private readonly SafetyFinder _safety;
    private readonly ILogger<BotModuleRunner> _logger;

    private DisconnectModule? _disconnect;
    private IModule? _activeModule;
    private string? _activeModuleId;

    public BotModuleRunner(
        ModuleContext context,
        IConfigApi config,
        SafetyFinder safety,
        ILogger<BotModuleRunner> logger)
    {
        _context = context;
        _config = config;
        _safety = safety;
        _logger = logger;
        _config.ProfileChanged += OnProfileChanged;
    }

    public string? ActiveModuleStatus { get; private set; }

    public void ScheduleDisconnect(long? pauseTimeMs, string reason) =>
        _disconnect = new DisconnectModule(pauseTimeMs, reason);

    public void Tick(bool isRunning, bool heroValid)
    {
        if (_disconnect is { IsComplete: true })
            _disconnect = null;

        if (!isRunning || !heroValid)
        {
            _activeModule?.OnTickStopped();
            ActiveModuleStatus = null;
            return;
        }

        if (_disconnect is not null)
        {
            _activeModule?.OnTickStopped();
            ActiveModuleStatus = _disconnect.Status;
            _disconnect.OnTickModule();
            return;
        }

        var module = EnsureActiveModule();
        if (module is null)
        {
            ActiveModuleStatus = "No active module";
            return;
        }

        if (!_safety.Tick())
        {
            ActiveModuleStatus = _safety.Status;
            return;
        }

        module.OnTickModule();
        ActiveModuleStatus = module.GetStatus();
    }

    private IModule? EnsureActiveModule()
    {
        var moduleId = _config.GetConfigValue<string>("general.current_module") ?? ModuleIds.Collector;

        if (_activeModule is not null && string.Equals(_activeModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
            return _activeModule;

        _activeModule = CreateModule(moduleId);
        _activeModuleId = moduleId;

        if (_activeModule is null)
            _logger.LogWarning("Unknown module id in config: {ModuleId}", moduleId);

        return _activeModule;
    }

    private IModule? CreateModule(string moduleId)
    {
        if (ModuleIds.IsCollector(moduleId))
            return new CollectorModule(_context, _safety);

        return null;
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        _activeModule = null;
        _activeModuleId = null;
    }
}
