using System.Globalization;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels.Shell;

/// <summary>Метрики title bar — только отображение, без собственного источника данных.</summary>
public sealed partial class TitleBarDiagnosticsViewModel : ViewModelBase
{
    [Reactive] private string _tickMsText = "—";
    [Reactive] private string _memoryMbText = "—";
    [Reactive] private string _pingText = "—";
    [Reactive] private string _fpsText = "—";

    public TitleBarDiagnosticsViewModel()
    {
        TickMsText = "0,4";
        MemoryMbText = "—";
        PingText = "—";
        FpsText = "—";
    }

    public void Apply(BotStatusSnapshot snapshot)
    {
        TickMsText = snapshot.LastTickMs.ToString("0.#", CultureInfo.CurrentCulture);
        PingText = snapshot.Ping.ToString(CultureInfo.CurrentCulture);
    }
}
