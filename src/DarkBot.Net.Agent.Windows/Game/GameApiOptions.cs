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

    /// <summary>darkDev avm_move.py HTTP port (default 44570).</summary>
    public int FridaApiPort { get; set; } = 44570;

    /// <summary>Path to Darkorbit-client repo (auto-detected if empty).</summary>
    public string DarkorbitClientPath { get; set; } = string.Empty;

    /// <summary>Seconds to wait for Pepper process after client launch.</summary>
    public int ClientConnectTimeoutSec { get; set; } = 180;

    /// <summary>Seconds to wait for GET /status ready after map load.</summary>
    public int FridaReadyTimeoutSec { get; set; } = 180;
}
