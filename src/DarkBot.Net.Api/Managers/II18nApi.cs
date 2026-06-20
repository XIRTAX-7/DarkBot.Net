namespace DarkBot.Net.Api.Managers;

/// <summary>Port of eu.darkbot.api.managers.I18nAPI (minimal subset).</summary>
public interface II18nApi : IApi.ISingleton
{
    string Get(string key);
    string Get(string key, params object[] args);
}
