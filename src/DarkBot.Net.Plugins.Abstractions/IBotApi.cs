using DarkBot.Net.Api;

namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.managers.BotAPI.</summary>
public interface IBotApi : IApi.ISingleton
{
    double TickTime { get; }
    bool IsRunning { get; }
    void SetRunning(bool running);
    IModule? Module { get; }
    IModule? NonTemporalModule { get; }
    T? SetModule<T>(T? module) where T : class, IModule;
}
