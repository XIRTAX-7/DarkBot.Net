namespace DarkBot.Net.Core.Models.Game;

/// <summary>Нормализованная фаза подключения к игре для UI-форматирования.</summary>
public enum GameConnectionStatusKind
{
    OnMapActive,
    Connecting,
    Authenticating,
    InHangar,
    EnteringMap,
    GameNotLaunched,
    Launching,
    WaitingLoad,
    WaitingConnection,
    LaunchFailed
}
