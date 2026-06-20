using DarkBot.Net.Api.Managers;

namespace DarkBot.Net.Core.Managers;

public sealed class I18nApi : II18nApi
{
    private static readonly Dictionary<string, string> Defaults = new(StringComparer.Ordinal)
    {
        ["module.disconnect.reason.draw_fire"] = "Enemy used draw fire",
        ["module.disconnect.reason.death_pause"] = "Killed by {0} {1} times"
    };

    public string Get(string key) => Defaults.GetValueOrDefault(key, key);

    public string Get(string key, params object[] args) =>
        string.Format(Get(key), args);
}
