namespace DarkBot.Net.Core.Config;

/// <summary>Политика записи: user UI — только configs/, AI — только configs/ai/.</summary>
public interface IConfigWritePolicy
{
    bool CanWrite(string profileName, ProfileOwner owner, ConfigActor actor);
    void EnsureCanWrite(string profileName, ProfileOwner owner, ConfigActor actor);
}
