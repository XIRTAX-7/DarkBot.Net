namespace DarkBot.Net.Ui.Configuration;

/// <summary>Local dev credentials from appsettings.Local.json (gitignored).</summary>
public sealed class TestLoginOptions
{
    public const string SectionName = "DarkBot:TestLogin";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
