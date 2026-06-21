namespace DarkBot.Net.Agent.Windows.Game;

public sealed class GameApiOptions
{
    public const string SectionName = "DarkBot";

    public GameApiMode BrowserApi { get; set; } = GameApiMode.FridaClient;

    public string LibPath { get; set; } = "./lib";

    public string ClassesPath { get; set; } = string.Empty;

    /// <summary>Optional path to DarkBot.jar (required by verifier AuthAPIImpl / setupAuth).</summary>
    public string DarkBotJarPath { get; set; } = string.Empty;

    /// <summary>JVM user.dir (default: app base directory, parent of ./lib).</summary>
    public string JvmWorkingDirectory { get; set; } = string.Empty;

    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public bool Use3D { get; set; }

    public bool UseProxy { get; set; }

    public bool ForceGameLanguage { get; set; }

    public string? GameLanguage { get; set; }

    /// <summary>darkDev avm_move.py bridge port — HTTP commands + WS status (default 44570).</summary>
    public int FridaApiPort { get; set; } = 44570;

    /// <summary>Expected WS ping interval from Frida bridge (seconds).</summary>
    public int FridaBridgeHeartbeatSec { get; set; } = 15;

    /// <summary>Mark bridge offline if no WS status/ping for this many seconds.</summary>
    public int FridaBridgeStaleSec { get; set; } = 30;

    /// <summary>Darkorbit-client control WS port (inject/controlServer.js, default 44568).</summary>
    public int ControlPort { get; set; } = 44568;

    /// <summary>packet_dumper.py WebSocket port (default 44569).</summary>
    public int PacketPort { get; set; } = 44569;

    /// <summary>Enable packet bridge from Darkorbit-client to .NET.</summary>
    public bool EnablePacketBridge { get; set; } = true;

    /// <summary>Path to Darkorbit-client repo (auto-detected if empty).</summary>
    public string DarkorbitClientPath { get; set; } = string.Empty;

    /// <summary>Seconds to wait for Pepper process after client launch.</summary>
    public int ClientConnectTimeoutSec { get; set; } = 180;

    /// <summary>Seconds to wait for Frida bridge ready (WS snapshot) after map load.</summary>
    public int FridaReadyTimeoutSec { get; set; } = 180;

    /// <summary>Milliseconds between Pepper process polls during connect.</summary>
    public int ConnectPollIntervalMs { get; set; } = 400;

    /// <summary>Milliseconds between Frida ready checks during connect (HTTP fallback only).</summary>
    public int FridaReadyPollIntervalMs { get; set; } = 500;

    /// <summary>Delay before Frida attach after page load (Darkorbit-client MovementTimeout).</summary>
    public int MovementTimeoutMs { get; set; } = 8000;
}
