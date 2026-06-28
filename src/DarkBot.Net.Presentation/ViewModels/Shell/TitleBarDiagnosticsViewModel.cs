using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Presentation.Formatting;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels.Shell;

/// <summary>
/// Read-only метрики title bar. Данные приходят из <see cref="TitleBarDiagnosticsUiCoordinator"/>.
/// </summary>
public sealed partial class TitleBarDiagnosticsViewModel : ViewModelBase
{
    private double _lastTickMs;
    private double _lastLoopHz;

    [Reactive] private string _tickMsText = TitleBarDiagnosticsFormatter.EmptyPlaceholder;
    [Reactive] private string _memoryMbText = TitleBarDiagnosticsFormatter.EmptyPlaceholder;
    [Reactive] private string _pingText = TitleBarDiagnosticsFormatter.EmptyPlaceholder;
    [Reactive] private string _fpsText = TitleBarDiagnosticsFormatter.EmptyPlaceholder;

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public TitleBarDiagnosticsViewModel()
    {
        _lastTickMs = 0.4;
        _lastLoopHz = 10;
        TickMsText = "0,4";
        MemoryMbText = "128";
        PingText = TitleBarDiagnosticsFormatter.EmptyPlaceholder;
        FpsText = "10";
    }

    public void Apply(BotDiagnosticsSnapshot snapshot)
    {
        if (snapshot.LastTickMs > 0)
            _lastTickMs = snapshot.LastTickMs;

        if (snapshot.LoopHz > 0)
            _lastLoopHz = snapshot.LoopHz;

        TickMsText = TitleBarDiagnosticsFormatter.FormatTickMs(
            snapshot.LastTickMs > 0 ? snapshot.LastTickMs : _lastTickMs);
        MemoryMbText = TitleBarDiagnosticsFormatter.FormatMemoryMb(snapshot.MemoryMb);
        PingText = TitleBarDiagnosticsFormatter.FormatPing(snapshot.Ping);
        FpsText = TitleBarDiagnosticsFormatter.FormatLoopHz(
            snapshot.LoopHz > 0 ? snapshot.LoopHz : _lastLoopHz);
    }
}
