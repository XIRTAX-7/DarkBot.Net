namespace DarkBot.Net.Core.Interfaces.Game;

/// <summary>Frida /status addresses for BotInstaller.</summary>
public interface IGameInstallerProbe
{
    void RefreshStatus();

    bool TryGetInstallerAddresses(
        out long mainApplicationAddress,
        out long mainAddress,
        out long screenManagerAddress,
        out long connectionManagerAddress);
}
