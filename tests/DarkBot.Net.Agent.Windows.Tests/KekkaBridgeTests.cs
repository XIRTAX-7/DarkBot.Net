using DarkBot.Net.Agent.Windows.Bridge;

namespace DarkBot.Net.Agent.Windows.Tests;

public class KekkaBridgeTests
{
    public KekkaBridgeTests() => NativeBridgeTestEnvironment.PreparePath();

    [Fact]
    public void Kekka_is_available_when_dll_present()
    {
        if (!NativeBridgeTestEnvironment.KekkaArtifactsAvailable)
            return;

        using var bridge = NativeBridgeTestEnvironment.CreateInitializedBridge();
        Assert.True(bridge.IsKekkaAvailable);
        Assert.True(bridge.Kekka.Version >= 0);
    }

    [Fact]
    public void Kekka_is_invalid_without_game_window()
    {
        if (!NativeBridgeTestEnvironment.KekkaArtifactsAvailable)
            return;

        using var bridge = NativeBridgeTestEnvironment.CreateInitializedBridge();
        Assert.False(bridge.Kekka.IsValid);
    }

    [Fact]
    public void Kekka_move_ship_does_not_throw_when_not_valid()
    {
        if (!NativeBridgeTestEnvironment.KekkaArtifactsAvailable)
            return;

        using var bridge = NativeBridgeTestEnvironment.CreateInitializedBridge();
        var exception = Record.Exception(() => bridge.Kekka.MoveShip(0, 10_000, 6_000));
        Assert.Null(exception);
    }
}
