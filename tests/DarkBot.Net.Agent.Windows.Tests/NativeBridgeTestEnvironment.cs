using System.Globalization;
using DarkBot.Net.Agent.Windows.Bridge;

namespace DarkBot.Net.Agent.Windows.Tests;

public static class NativeBridgeTestEnvironment
{
    public static void PreparePath()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var serverBin = Path.Combine(javaHome, "bin", "server");
            if (Directory.Exists(serverBin))
            {
                var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (!path.Contains(serverBin, StringComparison.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable("PATH", $"{serverBin};{path}");
            }
        }

        var libDir = NativeBridgePaths.ResolveLibDir();
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!pathValue.Contains(libDir, StringComparison.OrdinalIgnoreCase))
            Environment.SetEnvironmentVariable("PATH", $"{libDir};{pathValue}");
    }

    public static bool NativeArtifactsAvailable =>
        OperatingSystem.IsWindows()
        && File.Exists(Path.Combine(NativeBridgePaths.ResolveLibDir(), "DarkBotBridge.dll"))
        && File.Exists(Path.Combine(NativeBridgePaths.ResolveLibDir(), "DarkMemAPI.dll"))
        && Directory.Exists(NativeBridgePaths.ResolveClassesDir());

    public static bool KekkaArtifactsAvailable =>
        NativeArtifactsAvailable
        && File.Exists(Path.Combine(NativeBridgePaths.ResolveLibDir(), "KekkaPlayer.dll"));

    public static NativeGameBridge CreateInitializedBridge()
    {
        var bridge = new NativeGameBridge();
        var libDir = NativeBridgePaths.ResolveLibDir();
        NativeBridgePaths.EnsureDarkBotJarInLib(libDir, null);
        bridge.Initialize(
            libDir,
            NativeBridgePaths.BuildBridgeClassPath(NativeBridgePaths.ResolveClassesDir(), libDir));
        return bridge;
    }
}
