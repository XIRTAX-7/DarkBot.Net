namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Writes KekkaPlayerLauncher @launch.properties after login for standalone Java debugging.</summary>
public static class KekkaLaunchDebugWriter
{
    public static void TryWrite(GameLaunchParameters launch, string flashOcxPath, GameApiOptions options, int proxyPort)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "launch.properties");
            var vars = FlashVarBuilder.BuildVarsString(launch.FlashParams, options);

            var lines = new[]
            {
                $"flashOcx={flashOcxPath}",
                $"url={launch.InstanceUrl}",
                $"sid=dosid={launch.Sid}",
                $"preloader={launch.PreloaderUrl}",
                $"vars={vars}",
                $"width={options.Width}",
                $"height={options.Height}",
                $"proxyPort={proxyPort}",
            };

            File.WriteAllLines(path, lines);
        }
        catch
        {
            // Debug helper only — never block launch.
        }
    }
}
