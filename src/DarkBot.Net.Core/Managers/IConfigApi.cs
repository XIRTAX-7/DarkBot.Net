using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Core.Managers;

public interface IConfigApi : IApi.ISingleton
{
    IConfigSetting<object> ConfigRoot { get; }
    BotProfileDocument CurrentDocument { get; }
    ProfileOwner CurrentOwner { get; }
    IReadOnlyList<string> ConfigProfiles { get; }
    string CurrentProfile { get; }

    event EventHandler<ConfigProfileChangedEventArgs>? ProfileChanged;

    void SetConfigProfile(string profile);
    void ReloadProfile();
    void SetValue<T>(string path, T value, ConfigActor actor);
    Task SaveAsync(ConfigActor actor, CancellationToken cancellationToken = default);

    IConfigSetting<T>? GetConfig<T>(string path);
    IConfigSetting<T> RequireConfig<T>(string path);
    T? GetConfigValue<T>(string path);
    IReadOnlySet<string>? GetChildren(string path);
}
