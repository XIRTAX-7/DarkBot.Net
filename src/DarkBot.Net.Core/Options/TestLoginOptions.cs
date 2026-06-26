namespace DarkBot.Net.Core.Options;

/// <summary>Локальные dev-учётные данные из appsettings.Local.json (gitignored).</summary>
public sealed class TestLoginOptions
{
    public const string SectionName = "DarkBot:TestLogin";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
