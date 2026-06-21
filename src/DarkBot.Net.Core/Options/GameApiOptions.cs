namespace DarkBot.Net.Core.Options;

public sealed class GameApiOptions
{
    public const string SectionName = "DarkBot";

    public GameApiMode BrowserApi { get; set; } = GameApiMode.FridaClient;

    public string LibPath { get; set; } = "./lib";

    public string ClassesPath { get; set; } = string.Empty;

    public string DarkBotJarPath { get; set; } = string.Empty;

    public string JvmWorkingDirectory { get; set; } = string.Empty;

    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public bool Use3D { get; set; }

    public bool UseProxy { get; set; }

    public bool ForceGameLanguage { get; set; }

    public string? GameLanguage { get; set; }

    public int FridaApiPort { get; set; } = 44570;

    public int FridaBridgeHeartbeatSec { get; set; } = 15;

    public int FridaBridgeStaleSec { get; set; } = 30;

    public int ControlPort { get; set; } = 44568;

    public int PacketPort { get; set; } = 44569;

    public bool EnablePacketBridge { get; set; } = true;

    public string DarkorbitClientPath { get; set; } = string.Empty;

    public int ClientConnectTimeoutSec { get; set; } = 180;

    public int FridaReadyTimeoutSec { get; set; } = 180;

    public int ConnectPollIntervalMs { get; set; } = 400;

    public int FridaReadyPollIntervalMs { get; set; } = 500;

    public int MovementTimeoutMs { get; set; } = 8000;
}
