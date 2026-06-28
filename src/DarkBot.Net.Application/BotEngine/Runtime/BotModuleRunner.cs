using DarkBot.Net.Application.BotEngine.Modules;

namespace DarkBot.Net.Application.BotEngine.Runtime;

/// <summary>Встроенные модули бота (internal C# classes; plugins — Phase 8, см. docs/adr/001-internal-modules.md).</summary>
public sealed class BotModuleRunner
{
    private DisconnectModule? _disconnect;

    public void ScheduleDisconnect(long? pauseTimeMs, string reason) =>
        _disconnect = new DisconnectModule(pauseTimeMs, reason);

    public void Tick(bool isRunning, bool heroValid)
    {
        if (_disconnect is { IsComplete: true })
            _disconnect = null;

        if (!isRunning || !heroValid || _disconnect is null)
            return;

        _disconnect.OnTickModule();
    }
}
