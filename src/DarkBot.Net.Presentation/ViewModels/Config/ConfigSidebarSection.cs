namespace DarkBot.Net.Presentation.ViewModels.Config;

public enum ConfigSidebarSection
{
    Main,
    Collect,
    NpcKill,
    Pet,
    Group,
    Other,
    BotSettings,
    Plugins,
}

public sealed record ConfigSidebarItem(string Title, ConfigSidebarSection Section);
