using DarkBot.Net.Agent.Windows.Game;

namespace DarkBot.Net.Agent.Windows.Tests;

public class KekkaPlayerLogReaderTests
{
    [Fact]
    public void ReadRecentDiagnostics_reports_immediate_exit_when_log_has_no_followup_lines()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "darkbot-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var logPath = Path.Combine(tempDir, "2026-06-14_03-04-54_KekkaPlayer.log");
            File.WriteAllLines(logPath,
            [
                "[2026/06/14 03:04:54 | INFO] KekkaPlayer v27",
                "[2026/06/14 03:04:54 | INFO] Starting new message loop",
            ]);

            var diagnostics = KekkaPlayerLogReader.ReadRecentDiagnostics(tempDir, TimeSpan.FromMinutes(5));

            Assert.Single(diagnostics);
            Assert.Contains("no lines after", diagnostics[0], StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
