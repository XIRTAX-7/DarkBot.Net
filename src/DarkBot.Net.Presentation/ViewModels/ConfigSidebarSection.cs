namespace DarkBot.Net.Presentation.ViewModels;

public enum ConfigSidebarSection
{
    Main,
    Collect,
    NpcKill,
    Pet,
    Group,
    Other,
    BotSettings,
    Plugins
}

public sealed record ConfigSidebarItem(string Title, ConfigSidebarSection Section);
