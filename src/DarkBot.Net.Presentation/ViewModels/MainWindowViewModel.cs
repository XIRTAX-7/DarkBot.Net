using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Presentation.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBotControlAppService _bot;
    private readonly BotUiStateService _state;
    private readonly GameConnectionStatusService _gameStatus;
    private readonly IGameClientRestartAppService _clientRestart;

    [Reactive] private string _title = "DarkBot.Net";
    [Reactive] private bool _botRunning;
    [Reactive] private bool _canRestartClient;
    [Reactive] private string _runButtonText = "Start";
    [Reactive] private string _statusLine = "Ready";
    [Reactive] private string _gameStatusLine = "Game not launched";
    [Reactive] private string _backpageStatus = "unknown";
    [Reactive] private bool _backpageValid;
    [Reactive] private BotUiSnapshot _snapshot = new(
        false, false, 0, 0, 0, 0, 0, -1, "Загрузка", 21000, 13500,
        Array.Empty<MapPortalSnapshot>(),
        false, 0, 0, 0, 0, 0, 0, 0, "unknown", false);

    public MainWindowViewModel(
        IBotControlAppService bot,
        BotUiStateService state,
        GameConnectionStatusService gameStatus,
        IGameClientRestartAppService clientRestart)
    {
        _bot = bot;
        _state = state;
        _gameStatus = gameStatus;
        _clientRestart = clientRestart;
        _gameStatus.StatusChanged += RefreshGameStatus;
        Refresh();
    }

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public MainWindowViewModel()
    {
        _bot = null!;
        _state = null!;
        _gameStatus = null!;
        _clientRestart = null!;
        Title = "DarkBot.Net";
        StatusLine = "Ready — design mode";
        BackpageStatus = "valid";
    }

    public void Refresh()
    {
        Snapshot = _state.Capture();
        BotRunning = Snapshot.BotRunning;
        BackpageStatus = Snapshot.BackpageStatus;
        BackpageValid = Snapshot.BackpageValid;
        CanRestartClient = _clientRestart?.CanRestart ?? false;
        RefreshGameStatus();
        Title = BotRunning ? "DarkBot.Net — running" : "DarkBot.Net — paused";
        RunButtonText = BotRunning ? "Pause" : "Start";
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

    [ReactiveCommand]
    private void ToggleBot()
    {
        if (_bot.IsRunning)
            _bot.Pause();
        else
            _bot.Start();

        Refresh();
    }

    [ReactiveCommand]
    private void ToggleBotFromMap() => ToggleBot();

    [ReactiveCommand]
    private async Task RestartClient()
    {
        if (_clientRestart is null || !_clientRestart.CanRestart)
            return;

        await _clientRestart.RestartClientAsync();
        Refresh();
    }
}
