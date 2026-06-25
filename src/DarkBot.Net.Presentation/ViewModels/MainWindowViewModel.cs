using System.Reactive.Linq;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.Services;
using DarkBot.Net.Presentation.ViewModels.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBotControlAppService _bot;
    private readonly IMovementApi _movement;
    private readonly BotUiStateService _state;
    private readonly GameConnectionStatusService _gameStatus;
    private readonly IGameClientRestartAppService _clientRestart;
    private readonly IConfigWindowService _configWindow;
    private readonly IServiceProvider _services;
    private readonly ILogger<MainWindowViewModel>? _logger;
    private readonly IObservable<bool> _canRestartClientGate;

    [Reactive] private string _title = "DarkBot.Net";
    [Reactive] private bool _botRunning;
    [Reactive] private bool _canRestartClient;
    [Reactive] private string _runButtonText = "Start";
    [Reactive] private string _statusLine = "Ready";
    [Reactive] private string _gameStatusLine = "Game not launched";
    [Reactive] private BotUiSnapshot _snapshot = new(
        false, false, 0, 0, 0, 0, 0, -1, "Загрузка", 21000, 13500,
        Array.Empty<MapPortalSnapshot>(),
        false, 0, 0, 0, 0, 0, 0, 0,
        MapRenderSnapshot.Loading);

    public MainWindowViewModel(
        IBotControlAppService bot,
        IMovementApi movement,
        BotUiStateService state,
        GameConnectionStatusService gameStatus,
        IGameClientRestartAppService clientRestart,
        IConfigWindowService configWindow,
        IServiceProvider services,
        ILogger<MainWindowViewModel> logger)
    {
        _bot = bot;
        _movement = movement;
        _state = state;
        _gameStatus = gameStatus;
        _clientRestart = clientRestart;
        _configWindow = configWindow;
        _services = services;
        _logger = logger;
        _canRestartClientGate = this.WhenAnyValue(x => x.CanRestartClient);
        _gameStatus.StatusChanged += RefreshGameStatus;
        OpenConfigCommand.ThrownExceptions.Subscribe(ex =>
            _logger?.LogError(ex, "UI config: OpenConfigCommand failed"));
        Refresh();
    }

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public MainWindowViewModel()
    {
        _bot = null!;
        _movement = null!;
        _state = null!;
        _gameStatus = null!;
        _clientRestart = null!;
        _configWindow = null!;
        _services = null!;
        _canRestartClientGate = Observable.Return(false);
        Title = "DarkBot.Net";
        StatusLine = "Ready — design mode";
    }

    public void Refresh()
    {
        Snapshot = _state.Capture();
        BotRunning = Snapshot.BotRunning;
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
    private void OpenConfig()
    {
        _logger?.LogInformation("UI config: OpenConfig requested");
        _configWindow.Show();
    }

    [ReactiveCommand]
    private void OpenLogin() =>
        _services.GetRequiredService<ShellWindowViewModel>().ShowLogin();

    public void MoveShipToMapLocation(MapClickEventArgs click)
    {
        _logger?.LogInformation(
            "Map click screen=({ScreenX:F1},{ScreenY:F1}) game=({GameX:F1},{GameY:F1}) " +
            "frac=({FracX:F3},{FracY:F3}) hero=({HeroX:F0},{HeroY:F0}) heroFrac=({HeroFracX:F3},{HeroFracY:F3}) " +
            "minimap≈({MiniX:F0}/{MiniY:F0}) map={MapW}x{MapH}",
            click.ScreenX, click.ScreenY, click.GameX, click.GameY,
            click.MapWidth > 0 ? click.GameX / click.MapWidth : 0,
            click.MapHeight > 0 ? click.GameY / click.MapHeight : 0,
            click.HeroX, click.HeroY,
            click.MapWidth > 0 ? click.HeroX / click.MapWidth : 0,
            click.MapHeight > 0 ? click.HeroY / click.MapHeight : 0,
            click.GameX / 10.0, click.GameY / 10.0,
            click.MapWidth, click.MapHeight);
        _movement?.MoveTo(click.GameX, click.GameY);
    }

    [ReactiveCommand(CanExecute = nameof(_canRestartClientGate))]
    private async Task RestartClientAsync()
    {
        if (_clientRestart is null || !_clientRestart.CanRestart)
            return;

        await _clientRestart.RestartClientAsync();
        Refresh();
    }
}
