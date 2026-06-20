namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Port of FlashVarReplacement — bot overrides for flash embed vars.</summary>
public static class FlashVarBuilder
{
    public static string BuildVarsString(
        IReadOnlyDictionary<string, string> flashParams,
        GameApiOptions options)
    {
        var values = new Dictionary<string, string>(flashParams, StringComparer.Ordinal);

        values["autoStartEnabled"] = "1";
        values["display2d"] = options.Use3D ? "1" : "2";

        if (options.ForceGameLanguage && !string.IsNullOrWhiteSpace(options.GameLanguage))
            values["lang"] = options.GameLanguage!;

        return string.Join("&", values.Select(static pair => $"{pair.Key}={pair.Value}"));
    }
}
