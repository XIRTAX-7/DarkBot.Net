using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Application.Memory;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Presentation.Resources;

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
                return UiStrings.Status_OnMapActive;

            if (_bridge is not null)
            {
                return _bridge.RuntimePhase switch
                {
                    UnityBridgeRuntimePhase.Bootstrapping => UiStrings.Status_Connecting,
                    UnityBridgeRuntimePhase.Authenticating => UiStrings.Status_Authenticating,
                    UnityBridgeRuntimePhase.InHangar => UiStrings.Status_InHangar,
                    UnityBridgeRuntimePhase.EnteringMap => UiStrings.Status_EnteringMap,
                    UnityBridgeRuntimePhase.OnMap => UiStrings.Status_OnMapActive,
                    _ => FormatConnectionPhase()
                };
            }

            return FormatConnectionPhase();
        }
    }

    private string FormatConnectionPhase() =>
        _game.Phase switch
        {
            GameConnectionPhase.NotStarted => UiStrings.Status_GameNotLaunched,
            GameConnectionPhase.Launching => UiStrings.Status_Launching,
            GameConnectionPhase.WaitingForGameLoad => UiStrings.Status_WaitingLoad,
            GameConnectionPhase.Failed => FormatFailureStatus(),
            _ => UiStrings.Status_WaitingConnection
        };

    private string FormatFailureStatus()
    {
        if (!string.IsNullOrWhiteSpace(_game.LastFailureReason))
            return UiStrings.Format(nameof(UiStrings.Status_LaunchFailedWithReason), _game.LastFailureReason);

        return UiStrings.Status_LaunchFailed;
    }

    public string? FailureReason => _game.LastFailureReason;

    public bool IsGameConnected => _addresses.HasScreenManager;
}
