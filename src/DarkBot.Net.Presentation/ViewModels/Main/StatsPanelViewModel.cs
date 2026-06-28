using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Presentation.Formatting;
using DarkBot.Net.Presentation.Resources;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels.Main;

public sealed partial class StatsPanelViewModel : ViewModelBase
{
    [Reactive] private string _sessionStateText = UiStrings.Main_Stats_Paused;
    [Reactive] private string _runningTimeText = "00";
    [Reactive] private double _earnedCreditsPerHour;
    [Reactive] private double _earnedUridiumPerHour;
    [Reactive] private double _earnedExperiencePerHour;
    [Reactive] private double _earnedHonorPerHour;

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public StatsPanelViewModel()
    {
        SessionStateText = UiStrings.Main_Stats_Running;
        RunningTimeText = "01:23:45";
        EarnedCreditsPerHour = 1_250_000;
        EarnedUridiumPerHour = 42_500;
        EarnedExperiencePerHour = 987_654;
        EarnedHonorPerHour = 12_345;
    }

    public void Apply(BotStatusSnapshot snapshot)
    {
        SessionStateText = snapshot.BotRunning
            ? UiStrings.Main_Stats_Running
            : UiStrings.Main_Stats_Paused;
        RunningTimeText = StatsDisplayFormat.FormatRunningTime(snapshot.RunningTime);
        EarnedCreditsPerHour = snapshot.EarnedCreditsPerHour;
        EarnedUridiumPerHour = snapshot.EarnedUridiumPerHour;
        EarnedExperiencePerHour = snapshot.EarnedExperiencePerHour;
        EarnedHonorPerHour = snapshot.EarnedHonorPerHour;
    }
}
