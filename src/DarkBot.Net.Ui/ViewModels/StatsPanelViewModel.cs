using CommunityToolkit.Mvvm.ComponentModel;
using DarkBot.Net.Ui.Services;

namespace DarkBot.Net.Ui.ViewModels;

public sealed partial class StatsPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _credits;

    [ObservableProperty]
    private double _uridium;

    [ObservableProperty]
    private double _experience;

    [ObservableProperty]
    private double _honor;

    [ObservableProperty]
    private int _ping;

    [ObservableProperty]
    private long _tickCount;

    [ObservableProperty]
    private double _lastTickMs;

    [ObservableProperty]
    private string _runtime = "00:00:00";

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
