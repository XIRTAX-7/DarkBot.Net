namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Binary opcodes for Darkorbit-client control WebSocket (inject/controlServer.js).</summary>
public static class ElectronControlOpcodes
{
    public const short GetVersion = 1;
    public const short GetPepperPid = 2;
    public const short SetSize = 3;
    public const short SetVisible = 4;
    public const short SetMinimized = 5;
    public const short Reload = 6;
    public const short IsValid = 7;
}
