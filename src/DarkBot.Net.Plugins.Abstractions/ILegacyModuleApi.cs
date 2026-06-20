using DarkBot.Net.Api;

namespace DarkBot.Net.Plugins.Abstractions;

/// <summary>Port of eu.darkbot.shared.legacy.LegacyModuleAPI.</summary>
public interface ILegacyModuleApi : IApi.ISingleton
{
    IModule GetDisconnectModule(long? pauseTimeMs, string reason);
}
