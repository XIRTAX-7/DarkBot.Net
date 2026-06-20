using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Modules;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.Core.Managers;

public sealed class LegacyModuleApi : ILegacyModuleApi
{
    public IModule GetDisconnectModule(long? pauseTimeMs, string reason) =>
        new DisconnectModule(pauseTimeMs, reason);
}
