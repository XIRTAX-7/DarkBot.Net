using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Presentation.Formatting;

namespace DarkBot.Net.Presentation.Tests;

public sealed class TitleBarDiagnosticsFormatterTests
{
    [Fact]
    public void FormatTickMs_WhenPositive_ReturnsFormattedValue()
    {
        Assert.NotEqual(TitleBarDiagnosticsFormatter.EmptyPlaceholder, TitleBarDiagnosticsFormatter.FormatTickMs(0.4));
    }

    [Fact]
    public void FormatTickMs_WhenZero_ReturnsPlaceholder()
    {
        Assert.Equal("-", TitleBarDiagnosticsFormatter.FormatTickMs(0));
    }

    [Fact]
    public void ApplySnapshot_MapsAllMetrics()
    {
        var snapshot = new BotDiagnosticsSnapshot(
            LastTickMs: 100,
            MemoryMb: 256,
            Ping: 42,
            LoopHz: 10);

        Assert.Equal("100", TitleBarDiagnosticsFormatter.FormatTickMs(snapshot.LastTickMs));
        Assert.Equal("256", TitleBarDiagnosticsFormatter.FormatMemoryMb(snapshot.MemoryMb));
        Assert.Equal("42", TitleBarDiagnosticsFormatter.FormatPing(snapshot.Ping));
        Assert.Equal("10", TitleBarDiagnosticsFormatter.FormatLoopHz(snapshot.LoopHz));
    }
}
