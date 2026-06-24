namespace DarkBot.Net.Core.Options;

/// <summary>Game connection mode.</summary>
public enum GameApiMode
{
    /// <summary>Unity IL2CPP client (DarkOrbit.exe) + FridaCLR in-process bridge.</summary>
    UnityClient,

    /// <summary>Legacy Darkorbit-client (Electron) + darkDev Frida HTTP bridge.</summary>
    FridaClient,

    /// <summary>Removed — do not use.</summary>
    [Obsolete("KekkaPlayer/Java path is disabled. Use UnityClient.")]
    KekkaPlayer = 98,

    /// <summary>Removed — merged into FridaClient.</summary>
    [Obsolete("Use UnityClient.")]
    DarkMemAttach = 99,
}
