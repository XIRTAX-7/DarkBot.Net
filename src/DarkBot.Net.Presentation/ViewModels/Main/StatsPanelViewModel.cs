using DarkBot.Net.Application.DTOs.Responses.Bot;
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

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public StatsPanelViewModel()
    {
        Credits = 1_250_000;
        Uridium = 42_500;
        Experience = 987_654_321;
        Honor = 12_345;
    }

    public void Apply(BotStatusSnapshot snapshot)
    {
        Credits = snapshot.Credits;
        Uridium = snapshot.Uridium;
        Experience = snapshot.Experience;
        Honor = snapshot.Honor;
    }
}
