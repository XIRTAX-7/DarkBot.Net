using DarkBot.Net.Api.Config;

namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.api.extensions.Configurable.</summary>
public interface IConfigurable<T>
{
    void SetConfig(IConfigSetting<T> config);
}
