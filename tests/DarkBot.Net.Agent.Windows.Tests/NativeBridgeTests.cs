using DarkBot.Net.Agent.Windows.Bridge;

namespace DarkBot.Net.Agent.Windows.Tests;

public class NativeBridgeTests
{
    public NativeBridgeTests() => NativeBridgeTestEnvironment.PreparePath();

    [Fact]
    public void Bridge_init_and_get_version_when_native_artifacts_present()
    {
        if (!NativeBridgeTestEnvironment.NativeArtifactsAvailable)
            return;

        using var bridge = NativeBridgeTestEnvironment.CreateInitializedBridge();
        Assert.True(bridge.DarkMemVersion >= 0);
    }

    [Fact]
    public void Bridge_read_int_without_attached_process_returns_zero()
    {
        if (!NativeBridgeTestEnvironment.NativeArtifactsAvailable)
            return;

        using var bridge = NativeBridgeTestEnvironment.CreateInitializedBridge();
        Assert.Equal(0, bridge.ReadInt(0x1000));
    }
}
