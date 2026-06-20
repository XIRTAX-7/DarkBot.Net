namespace DarkBot.Net.Login;

public sealed class LoginOptions
{
    public const string SectionName = "DarkBot";

    public string BackpageSidecarPath { get; set; } = "./sidecars/backpage/dark_backpage.exe";
    public string BackpageSidecarMinVersion { get; set; } = "1.3.0";
}
