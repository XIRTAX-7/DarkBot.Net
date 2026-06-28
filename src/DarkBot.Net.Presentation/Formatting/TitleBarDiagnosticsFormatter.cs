using System.Globalization;

namespace DarkBot.Net.Presentation.Formatting;

/// <summary>Форматирование метрик title bar для отображения в Shell.</summary>
public static class TitleBarDiagnosticsFormatter
{
    /// <summary>Плейсхолдер без данных — как в Java DiagnosticBar.</summary>
    public const string EmptyPlaceholder = "-";

    public static string FormatTickMs(double lastTickMs) =>
        lastTickMs > 0
            ? lastTickMs.ToString("0.#", CultureInfo.CurrentCulture)
            : EmptyPlaceholder;

    public static string FormatMemoryMb(double memoryMb) =>
        memoryMb > 0
            ? memoryMb.ToString("0", CultureInfo.CurrentCulture)
            : EmptyPlaceholder;

    public static string FormatPing(int ping) =>
        ping > 0
            ? ping.ToString(CultureInfo.CurrentCulture)
            : EmptyPlaceholder;

    public static string FormatLoopHz(double loopHz) =>
        loopHz > 0
            ? loopHz.ToString("0.#", CultureInfo.CurrentCulture)
            : EmptyPlaceholder;
}
