using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DarkBot.Net.Core.Bot;
using DarkBot.Net.Ui.Services;

namespace DarkBot.Net.Ui.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBotController _bot;
    private readonly BotUiStateService _state;
    private readonly GameConnectionStatusService _gameStatus;

    [ObservableProperty]
    private string _title = "DarkBot.Net";

    [ObservableProperty]
    private bool _botRunning;

    public string RunButtonText => BotRunning ? "Pause" : "Start";

    [ObservableProperty]
    private string _statusLine = "Ready";

    [ObservableProperty]
    private string _gameStatusLine = "Game not launched";

    [ObservableProperty]
    private string _backpageStatus = "unknown";

    [ObservableProperty]
    private bool _backpageValid;

    [ObservableProperty]
    private BotUiSnapshot _snapshot = new(
        false, false, 0, 0, 0, 0, 0, -1, "Загрузка", 21000, 13500,
        Array.Empty<MapPortalSnapshot>(),
        false, 0, 0, 0, 0, 0, 0, 0, "unknown", false);

    public MainWindowViewModel(
        IBotController bot,
        BotUiStateService state,
        GameConnectionStatusService gameStatus)
    {
        _bot = bot;
        _state = state;
        _gameStatus = gameStatus;
        _gameStatus.StatusChanged += () => RefreshGameStatus();
        Refresh();
    }

    public void Refresh()
    {
        Snapshot = _state.Capture();
        BotRunning = Snapshot.BotRunning;
        BackpageStatus = Snapshot.BackpageStatus;
        BackpageValid = Snapshot.BackpageValid;
        RefreshGameStatus();
        Title = BotRunning ? "DarkBot.Net — running" : "DarkBot.Net — paused";
        OnPropertyChanged(nameof(RunButtonText));
    }

    private void RefreshGameStatus()
    {
        GameStatusLine = _gameStatus.StatusLine;
        StatusLine = Snapshot.HeroValid
            ? $"{Snapshot.MapName} — HP {Snapshot.HeroHp}/{Snapshot.HeroMaxHp} — tick {Snapshot.LastTickMs:0.#} ms"
            : Snapshot.HeroOnMap
                ? $"{Snapshot.MapName} — ({Snapshot.HeroX:0}, {Snapshot.HeroY:0}) — {_gameStatus.StatusLine}"
                : Snapshot.MapId == -1
                ? _gameStatus.StatusLine
                : $"{Snapshot.MapName} — {_gameStatus.StatusLine}";
    }

    [RelayCommand]
    private void ToggleBot()
    {
        if (_bot.IsRunning)
            _bot.Pause();
        else
            _bot.Start();

        Refresh();
    }

    [RelayCommand]
    private void ToggleBotFromMap()
    {
        ToggleBot();
    }
}
