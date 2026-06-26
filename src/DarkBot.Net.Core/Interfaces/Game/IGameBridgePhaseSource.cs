using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Core.Interfaces.Game;

/// <summary>Источник runtime-фазы Unity/Frida bridge для application-слоя.</summary>
public interface IGameBridgePhaseSource
{
    UnityBridgeRuntimePhase RuntimePhase { get; }

    event Action? StatusChanged;
}
