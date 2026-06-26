using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Application.Services.Game;

public sealed class GameConnectionStatusAppService : IGameConnectionStatusAppService
{
    private readonly IGameConnection _game;
    private readonly IGameBridgePhaseSource? _bridge;
    private readonly BotAddressRegistry _addresses;

    public GameConnectionStatusAppService(
        IGameConnection game,
        BotAddressRegistry addresses,
        IGameBridgePhaseSource? bridge = null)
    {
        _game = game;
        _addresses = addresses;
        _bridge = bridge;
        _game.PhaseChanged += _ => StatusChanged?.Invoke();
        if (_bridge is not null)
            _bridge.StatusChanged += () => StatusChanged?.Invoke();
    }

    public event Action? StatusChanged;

    public GameConnectionStatusSnapshot GetStatus()
    {
        if (_addresses.HasScreenManager)
            return new GameConnectionStatusSnapshot(GameConnectionStatusKind.OnMapActive, null);

        if (_bridge is not null)
        {
            var bridgeKind = _bridge.RuntimePhase switch
            {
                UnityBridgeRuntimePhase.Bootstrapping => GameConnectionStatusKind.Connecting,
                UnityBridgeRuntimePhase.Authenticating => GameConnectionStatusKind.Authenticating,
                UnityBridgeRuntimePhase.InHangar => GameConnectionStatusKind.InHangar,
                UnityBridgeRuntimePhase.EnteringMap => GameConnectionStatusKind.EnteringMap,
                UnityBridgeRuntimePhase.OnMap => GameConnectionStatusKind.OnMapActive,
                _ => (GameConnectionStatusKind?)null
            };

            if (bridgeKind is not null)
                return new GameConnectionStatusSnapshot(bridgeKind.Value, null);
        }

        return MapConnectionPhase(_game.Phase, _game.LastFailureReason);
    }

    private static GameConnectionStatusSnapshot MapConnectionPhase(
        GameConnectionPhase phase,
        string? failureReason) =>
        phase switch
        {
            GameConnectionPhase.NotStarted => new(GameConnectionStatusKind.GameNotLaunched, null),
            GameConnectionPhase.Launching => new(GameConnectionStatusKind.Launching, null),
            GameConnectionPhase.WaitingForGameLoad => new(GameConnectionStatusKind.WaitingLoad, null),
            GameConnectionPhase.Failed => new(GameConnectionStatusKind.LaunchFailed, failureReason),
            _ => new(GameConnectionStatusKind.WaitingConnection, null)
        };
}
