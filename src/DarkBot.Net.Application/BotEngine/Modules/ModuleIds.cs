namespace DarkBot.Net.Application.BotEngine.Modules;

/// <summary>Идентификаторы встроенных модулей (general.current_module в profile).</summary>
public static class ModuleIds
{
    public const string Collector = "DarkBot.Net.Application.BotEngine.Modules.CollectorModule";

    public static bool IsCollector(string? moduleId) =>
        !string.IsNullOrWhiteSpace(moduleId)
        && (moduleId.Equals(Collector, StringComparison.OrdinalIgnoreCase)
            || moduleId.Equals("Collector", StringComparison.OrdinalIgnoreCase));
}
