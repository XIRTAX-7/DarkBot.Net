using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Core.Managers;

public interface IConfigApi : IApi.ISingleton
{
    IConfigSetting<object> ConfigRoot { get; }
    IReadOnlyList<string> ConfigProfiles { get; }
    string CurrentProfile { get; }
    void SetConfigProfile(string profile);

    IConfigSetting<T>? GetConfig<T>(string path);
    IConfigSetting<T> RequireConfig<T>(string path);
    T? GetConfigValue<T>(string path);
    IReadOnlySet<string>? GetChildren(string path);
}
