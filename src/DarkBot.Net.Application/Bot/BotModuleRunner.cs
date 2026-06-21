using DarkBot.Net.Application.Modules;

namespace DarkBot.Net.Application.Bot;

/// <summary>Встроенные модули бота (без plugin registry).</summary>
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
