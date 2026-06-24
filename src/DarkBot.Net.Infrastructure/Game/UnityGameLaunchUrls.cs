namespace DarkBot.Net.Infrastructure.Game;

/// <summary>URL для автологина Unity-клиента (те же action, что в GameLoadingManager.GetPost).</summary>
internal static class UnityGameLaunchUrls
{
    public const string WebGlAction = "internalWebGL";
    public const string MapRevolutionAction = "internalMapRevolution";

    public static string BuildWebGlLoginUrl(Uri instanceUri) =>
        BuildInternalActionUrl(instanceUri, WebGlAction);

    public static string BuildMapRevolutionUrl(Uri instanceUri) =>
        BuildInternalActionUrl(instanceUri, MapRevolutionAction);

    private static string BuildInternalActionUrl(Uri instanceUri, string action) =>
        $"{instanceUri.GetLeftPart(UriPartial.Authority)}/indexInternal.es?action={action}";
}
