using System.Reactive.Linq;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Presentation.Controls.Main.MapCanvas;
using DarkBot.Net.Presentation.Formatting;
using DarkBot.Net.Presentation.Resources;
using DarkBot.Net.Presentation.Ui.Config;
using DarkBot.Net.Presentation.ViewModels.Shared;
using DarkBot.Net.Presentation.ViewModels.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels.Main;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBotControlAppService _bot;
    private readonly IMovementAppService _movement;
    private readonly IBotStatusAppService _botStatus;
    private readonly IGameConnectionStatusAppService _gameStatus;
    private readonly IGameClientRestartAppService _clientRestart;
    private readonly IConfigWindowService _configWindow;
    private readonly IServiceProvider _services;
    private readonly ILogger<MainWindowViewModel>? _logger;
    private readonly IObservable<bool> _canRestartClientGate;

    [Reactive] private string _title = UiStrings.App_Title;
    [Reactive] private bool _botRunning;
    [Reactive] private bool _canRestartClient;
    [Reactive] private string _runButtonText = UiStrings.Main_StartButton;
    [Reactive] private string _statusLine = UiStrings.Main_Ready;
    [Reactive] private string _gameStatusLine = UiStrings.Status_GameNotLaunched;
    [Reactive] private BotStatusSnapshot _snapshot = new(
        false, 0, 0, 0, 0, 0, 0, 0,
        MapStatusSnapshot.Loading);

    public MainWindowViewModel(
        IBotControlAppService bot,
        IMovementAppService movement,
        IBotStatusAppService botStatus,
        IGameConnectionStatusAppService gameStatus,
        IGameClientRestartAppService clientRestart,
        IConfigWindowService configWindow,
        IServiceProvider services,
        ILogger<MainWindowViewModel> logger)
    {
        _bot = bot;
        _movement = movement;
        _botStatus = botStatus;
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
        _botStatus = null!;
        _gameStatus = null!;
        _clientRestart = null!;
        _configWindow = null!;
        _services = null!;
        _canRestartClientGate = Observable.Return(false);
        Title = UiStrings.App_Title;
        StatusLine = UiStrings.Main_ReadyDesignMode;
    }

    public void Refresh()
    {
        Snapshot = _botStatus.Capture();
        BotRunning = Snapshot.BotRunning;
        CanRestartClient = _clientRestart?.CanRestart ?? false;
        RefreshGameStatus();
        Title = BotRunning ? UiStrings.App_TitleRunning : UiStrings.App_TitlePaused;
        RunButtonText = BotRunning ? UiStrings.Main_PauseButton : UiStrings.Main_StartButton;
    }

    private void RefreshGameStatus()
    {
        var map = Snapshot.Map;
        var connectionStatus = _gameStatus.GetStatus();
        GameStatusLine = GameConnectionStatusFormatter.Format(connectionStatus);
        StatusLine = map.Hero.Valid
            ? UiStrings.Format(
                nameof(UiStrings.Status_HpFormat),
                map.MapName,
                map.Hero.Hp,
                map.Hero.MaxHp,
                Snapshot.LastTickMs.ToString("0.#", System.Globalization.CultureInfo.CurrentCulture))
            : map.Hero.OnMap
                ? UiStrings.Format(
                    nameof(UiStrings.Status_PositionFormat),
                    map.MapName,
                    map.Hero.X,
                    map.Hero.Y,
                    GameStatusLine)
                : map.MapId == -1
                    ? GameStatusLine
                    : UiStrings.Format(
                        nameof(UiStrings.Status_MapFormat),
                        map.MapName,
                        GameStatusLine);
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
        _movement.MoveTo(click.GameX, click.GameY);
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
