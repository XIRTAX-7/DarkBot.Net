namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Источник снимка состояния Frida/Unity bridge.</summary>
public interface IGameBridgeStatusSource
{
    FridaBridgeStatus? CurrentStatus { get; }

    bool RefreshStatus();

    event Action? StatusChanged;
}
