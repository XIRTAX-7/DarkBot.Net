namespace DarkBot.Net.Application.BotEngine.Modules;

/// <summary>Контракт игрового модуля бота (порт Java eu.darkbot.api.extensions.Module).</summary>
public interface IModule
{
    void OnTickModule();

    void OnTickStopped() { }

    bool CanRefresh() => true;

    string? GetStatus() => null;
}
