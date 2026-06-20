namespace DarkBot.Net.Plugins;

public sealed class PluginOptions
{
    public const string SectionName = "DarkBot";

    public string PluginsPath { get; set; } = "./plugins";
    public bool EnableHotReload { get; set; } = true;
}
