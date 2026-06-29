namespace DarkBot.Net.Core.Config;

/// <summary>Сессия активного профиля и переключение User ↔ AI (задел под MCP).</summary>
public interface IConfigSession
{
    ProfileOwner ActiveOwner { get; }
    string ActiveProfile { get; }
    string LastUserProfile { get; }
    string LastAiProfile { get; }

    event EventHandler? SessionChanged;

    void SwitchToUserProfile(string profileName);
    void SwitchToAiProfile(string profileName);
    void SwitchToAiControl(string aiProfileName);
    void RestoreUserControl();
}
