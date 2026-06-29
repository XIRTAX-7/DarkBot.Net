namespace DarkBot.Net.Core.Config;

/// <summary>Индекс сессии (%LocalAppData%/DarkBot.Net/session.json).</summary>
public sealed record BotConfigIndex(
    string LastUserProfile,
    string LastAiProfile,
    ProfileOwner ActiveOwner,
    string ActiveProfile)
{
    public static BotConfigIndex CreateDefault() =>
        new(
            ConfigProfileNames.DefaultUser,
            ConfigProfileNames.DefaultAiPve,
            ProfileOwner.User,
            ConfigProfileNames.DefaultUser);
}
