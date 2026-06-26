using DarkBot.Net.Presentation.Services;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels.Main;

public sealed partial class StatsPanelViewModel : ViewModelBase
{
    [Reactive] private double _credits;
    [Reactive] private double _uridium;
    [Reactive] private double _experience;
    [Reactive] private double _honor;
    [Reactive] private int _ping;
    [Reactive] private long _tickCount;
    [Reactive] private double _lastTickMs;
    [Reactive] private string _runtime = "00:00:00";

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public StatsPanelViewModel()
    {
        Credits = 1_250_000;
        Uridium = 42_500;
        Experience = 987_654_321;
        Honor = 12_345;
        Ping = 48;
        TickCount = 3600;
        LastTickMs = 2.4;
        Runtime = "00:06:00";
    }

    public void Apply(BotUiSnapshot snapshot)
    {
        Credits = snapshot.Credits;
        Uridium = snapshot.Uridium;
        Experience = snapshot.Experience;
        Honor = snapshot.Honor;
        Ping = snapshot.Ping;
        TickCount = snapshot.TickCount;
        LastTickMs = snapshot.LastTickMs;
        Runtime = TimeSpan.FromMilliseconds(snapshot.TickCount * 100).ToString(@"hh\:mm\:ss");
    }
}
