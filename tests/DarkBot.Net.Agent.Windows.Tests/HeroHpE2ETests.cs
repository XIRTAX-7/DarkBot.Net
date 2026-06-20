using System.Globalization;
using DarkBot.Net.Agent.Windows.Bridge;

namespace DarkBot.Net.Agent.Windows.Tests;

public class HeroHpE2ETests
{
    public HeroHpE2ETests() => NativeBridgeTestEnvironment.PreparePath();

    /// <summary>
    /// Optional live-game test. Set env vars before running:
    /// DARKBOT_GAME_PID=12345 DARKBOT_SHIP_ADDRESS=1A2B3C4D5E6F
    /// </summary>
    [Fact]
    public void E2E_read_hero_hp_when_game_attached()
    {
        if (!NativeBridgeTestEnvironment.NativeArtifactsAvailable)
            return;

        var pidText = Environment.GetEnvironmentVariable("DARKBOT_GAME_PID");
        var shipAddressText = Environment.GetEnvironmentVariable("DARKBOT_SHIP_ADDRESS");
        if (string.IsNullOrWhiteSpace(pidText) || string.IsNullOrWhiteSpace(shipAddressText))
            return;

        if (!int.TryParse(pidText, out var pid))
            return;

        var shipAddress = ParseAddress(shipAddressText);
        if (shipAddress <= 0)
            return;

        using var bridge = NativeBridgeTestEnvironment.CreateInitializedBridge();
        bridge.OpenProcess(pid);

        var hp = bridge.ReadHeroHp(shipAddress);
        Assert.InRange(hp, 1, int.MaxValue);
    }

    private static long ParseAddress(string text)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return long.Parse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : long.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
