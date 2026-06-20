namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Game connection mode. v1 supports Darkorbit-client + Frida only.</summary>
public enum GameApiMode
{
    /// <summary>Darkorbit-client (Electron) + darkDev Frida HTTP + DarkMem attach.</summary>
    FridaClient,

    /// <summary>Backpage session only — no game client.</summary>
    BackpageOnly,

    /// <summary>Removed — do not use.</summary>
    [Obsolete("KekkaPlayer/Java path is disabled. Use FridaClient.")]
    KekkaPlayer = 98,

    /// <summary>Removed — merged into FridaClient.</summary>
    [Obsolete("Use FridaClient.")]
    DarkMemAttach = 99,
}
