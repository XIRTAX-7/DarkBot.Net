using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Infrastructure.Config;

public sealed class ConfigWritePolicy : IConfigWritePolicy
{
    public bool CanWrite(string profileName, ProfileOwner owner, ConfigActor actor) =>
        actor switch
        {
            ConfigActor.User => owner is ProfileOwner.User,
            ConfigActor.Ai => owner is ProfileOwner.Ai,
            _ => false
        };

    public void EnsureCanWrite(string profileName, ProfileOwner owner, ConfigActor actor)
    {
        if (!CanWrite(profileName, owner, actor))
        {
            throw new UnauthorizedAccessException(
                $"Actor '{actor}' cannot write profile '{profileName}' (owner={owner}).");
        }
    }
}
