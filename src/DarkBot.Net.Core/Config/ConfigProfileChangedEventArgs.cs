namespace DarkBot.Net.Core.Config;

public sealed class ConfigProfileChangedEventArgs(string profileName, ProfileOwner owner) : EventArgs
{
    public string ProfileName { get; } = profileName;
    public ProfileOwner Owner { get; } = owner;
}
