using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Application.Memory;

namespace DarkBot.Net.Presentation.Services;

public sealed class GameConnectionStatusService
{
    private readonly IGameConnection _game;
    private readonly BotAddressRegistry _addresses;

    public GameConnectionStatusService(IGameConnection game, BotAddressRegistry addresses)
    {
        _game = game;
        _addresses = addresses;
        _game.PhaseChanged += _ => StatusChanged?.Invoke();
    }

    public event Action? StatusChanged;

    public string StatusLine
    {
        get
        {
            if (_game.Mode == GameApiMode.BackpageOnly)
                return "Backpage-only mode";

            if (_addresses.HasScreenManager)
                return "Game connected";

            return _game.Phase switch
            {
                GameConnectionPhase.NotStarted => "Game not launched",
                GameConnectionPhase.Launching => "Launching game…",
                GameConnectionPhase.WaitingForGameLoad => "Waiting for game load…",
                GameConnectionPhase.Failed => FormatFailureStatus(),
                _ => "Waiting for game connection…"
            };
        }
    }

    private string FormatFailureStatus()
    {
        if (!string.IsNullOrWhiteSpace(_game.LastFailureReason))
            return "Game launch failed: " + _game.LastFailureReason;

        return "Game launch failed";
    }

    public string? FailureReason => _game.LastFailureReason;

    public bool IsGameConnected => _addresses.HasScreenManager;
}
