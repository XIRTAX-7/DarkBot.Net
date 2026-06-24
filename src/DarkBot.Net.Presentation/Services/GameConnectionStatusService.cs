using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Application.Memory;
using DarkBot.Net.Infrastructure.Game.Bridge;

namespace DarkBot.Net.Presentation.Services;

public sealed class GameConnectionStatusService
{
    private readonly IGameConnection _game;
    private readonly IGameBridgeStatusSource? _bridge;
    private readonly BotAddressRegistry _addresses;

    public GameConnectionStatusService(
        IGameConnection game,
        BotAddressRegistry addresses,
        IGameBridgeStatusSource? bridge = null)
    {
        _game = game;
        _addresses = addresses;
        _bridge = bridge;
        _game.PhaseChanged += _ => StatusChanged?.Invoke();
        if (_bridge is not null)
            _bridge.StatusChanged += () => StatusChanged?.Invoke();
    }

    public event Action? StatusChanged;

    public string StatusLine
    {
        get
        {
            if (_addresses.HasScreenManager)
                return "On map — bot active";

            if (_bridge is not null)
            {
                return _bridge.RuntimePhase switch
                {
                    UnityBridgeRuntimePhase.Bootstrapping => "Connecting to game…",
                    UnityBridgeRuntimePhase.Authenticating => "Authorizing…",
                    UnityBridgeRuntimePhase.InHangar => "Hangar — entering map…",
                    UnityBridgeRuntimePhase.EnteringMap => "Loading map…",
                    UnityBridgeRuntimePhase.OnMap => "On map — bot active",
                    _ => FormatConnectionPhase()
                };
            }

            return FormatConnectionPhase();
        }
    }

    private string FormatConnectionPhase() =>
        _game.Phase switch
        {
            GameConnectionPhase.NotStarted => "Game not launched",
            GameConnectionPhase.Launching => "Launching game…",
            GameConnectionPhase.WaitingForGameLoad => "Waiting for game load…",
            GameConnectionPhase.Failed => FormatFailureStatus(),
            _ => "Waiting for game connection…"
        };

    private string FormatFailureStatus()
    {
        if (!string.IsNullOrWhiteSpace(_game.LastFailureReason))
            return "Game launch failed: " + _game.LastFailureReason;

        return "Game launch failed";
    }

    public string? FailureReason => _game.LastFailureReason;

    public bool IsGameConnected => _addresses.HasScreenManager;
}
