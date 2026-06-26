using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Presentation.Resources;

namespace DarkBot.Net.Presentation.Formatting;

public static class GameConnectionStatusFormatter
{
    public static string Format(GameConnectionStatusSnapshot status) =>
        status.Kind switch
        {
            GameConnectionStatusKind.OnMapActive => UiStrings.Status_OnMapActive,
            GameConnectionStatusKind.Connecting => UiStrings.Status_Connecting,
            GameConnectionStatusKind.Authenticating => UiStrings.Status_Authenticating,
            GameConnectionStatusKind.InHangar => UiStrings.Status_InHangar,
            GameConnectionStatusKind.EnteringMap => UiStrings.Status_EnteringMap,
            GameConnectionStatusKind.GameNotLaunched => UiStrings.Status_GameNotLaunched,
            GameConnectionStatusKind.Launching => UiStrings.Status_Launching,
            GameConnectionStatusKind.WaitingLoad => UiStrings.Status_WaitingLoad,
            GameConnectionStatusKind.WaitingConnection => UiStrings.Status_WaitingConnection,
            GameConnectionStatusKind.LaunchFailed => FormatFailure(status.FailureReason),
            _ => UiStrings.Status_WaitingConnection
        };

    private static string FormatFailure(string? failureReason)
    {
        if (!string.IsNullOrWhiteSpace(failureReason))
            return UiStrings.Format(nameof(UiStrings.Status_LaunchFailedWithReason), failureReason);

        return UiStrings.Status_LaunchFailed;
    }
}
