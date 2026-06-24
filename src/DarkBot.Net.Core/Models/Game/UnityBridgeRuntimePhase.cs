namespace DarkBot.Net.Core.Models.Game;

/// <summary>
/// Фаза Unity bridge внутри одной Frida-сессии.
/// Auth и карта — последовательные этапы одного бота, без detach между ними.
/// </summary>
public enum UnityBridgeRuntimePhase
{
    Unattached,
    Bootstrapping,
    Authenticating,
    InHangar,
    EnteringMap,
    OnMap
}
