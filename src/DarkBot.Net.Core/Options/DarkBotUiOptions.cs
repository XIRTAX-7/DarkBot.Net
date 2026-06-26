namespace DarkBot.Net.Core.Options;

/// <summary>Конфигурация приложения DarkBot (секция <c>DarkBot</c> в appsettings).</summary>
public sealed class DarkBotUiOptions
{
    public const string SectionName = "DarkBot";

    public string LibPath { get; set; } = "./lib";

    public string ClassesPath { get; set; } = string.Empty;

    public string DarkBotJarPath { get; set; } = string.Empty;

    public string VerifierPath { get; set; } = "./lib/verifier.jar";

    public int VerifierPort { get; set; } = 8091;

    public string ProfilesPath { get; set; } = "./configs";

    public bool VerifierDevBypass { get; set; } = true;

    public string LogPath { get; set; } = "./logs/darkbot-.log";

    public GameApiMode BrowserApi { get; set; } = GameApiMode.UnityClient;

    public int FridaApiPort { get; set; } = 44570;

    public string DarkorbitClientPath { get; set; } = string.Empty;

    public int GameWidth { get; set; } = 1280;

    public int GameHeight { get; set; } = 720;

    public bool Use3D { get; set; }

    public bool UseProxy { get; set; }

    public bool ForceGameLanguage { get; set; }

    public string? GameLanguage { get; set; }
}
