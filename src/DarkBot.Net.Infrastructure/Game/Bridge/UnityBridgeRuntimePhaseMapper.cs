using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Определяет runtime-фазу bridge по снимку agent getStatus().</summary>
public static class UnityBridgeRuntimePhaseMapper
{
    public static UnityBridgeRuntimePhase FromAgentStatus(UnityBridgeAgentStatus? status, bool isAttached)
    {
        if (!isAttached || status is null)
            return UnityBridgeRuntimePhase.Unattached;

        if (!status.BootstrapHooksReady)
            return UnityBridgeRuntimePhase.Bootstrapping;

        if (status.Ready || status.MovementHooksReady)
            return UnityBridgeRuntimePhase.OnMap;

        if (status.MapStartRequested || status.MapStartComplete)
            return UnityBridgeRuntimePhase.EnteringMap;

        if (status.SessionInjected || status.HangarDataReadyAt > 0 || status.LaunchShowStarted)
            return UnityBridgeRuntimePhase.InHangar;

        return UnityBridgeRuntimePhase.Authenticating;
    }
}
