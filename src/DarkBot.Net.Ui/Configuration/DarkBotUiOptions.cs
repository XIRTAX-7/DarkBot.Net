namespace DarkBot.Net.Ui.Configuration;

public sealed class DarkBotUiOptions
{
    public const string SectionName = "DarkBot";

    public string LibPath { get; set; } = "./lib";
    public string ClassesPath { get; set; } = string.Empty;

    public string DarkBotJarPath { get; set; } = string.Empty;
    public string PluginsPath { get; set; } = "./plugins";
    public string VerifierPath { get; set; } = "./lib/verifier.jar";
    public int VerifierPort { get; set; } = 8091;
    public string ProfilesPath { get; set; } = "./configs";
    public bool VerifierDevBypass { get; set; } = true;
    public string BackpageSidecarPath { get; set; } = "./sidecars/backpage/dark_backpage.exe";
    public string BackpageSidecarMinVersion { get; set; } = "1.3.0";
    public string LogPath { get; set; } = "./logs/darkbot-.log";

    public Agent.Windows.Game.GameApiMode BrowserApi { get; set; } = Agent.Windows.Game.GameApiMode.FridaClient;
    public int FridaApiPort { get; set; } = 44570;
    public string DarkorbitClientPath { get; set; } = string.Empty;
    public int GameWidth { get; set; } = 1280;
    public int GameHeight { get; set; } = 720;
    public bool Use3D { get; set; }
    public bool UseProxy { get; set; }
    public bool ForceGameLanguage { get; set; }
    public string? GameLanguage { get; set; }
}
