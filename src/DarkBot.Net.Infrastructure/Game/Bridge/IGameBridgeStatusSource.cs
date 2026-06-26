using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Источник снимка состояния Frida/Unity bridge.</summary>
public interface IGameBridgeStatusSource : IGameBridgePhaseSource
{
    FridaBridgeStatus? CurrentStatus { get; }

    UnityBridgeAgentStatus? AgentStatus { get; }

    UnityBridgeRuntimePhase RuntimePhase { get; }

    bool RefreshStatus();

    event Action? StatusChanged;
}
