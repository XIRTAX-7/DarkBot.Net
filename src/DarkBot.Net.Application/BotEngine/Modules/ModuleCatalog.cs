namespace DarkBot.Net.Application.BotEngine.Modules;

/// <summary>Доступные встроенные модули для UI (general.current_module).</summary>
public static class ModuleCatalog
{
    public static IReadOnlyList<ModuleOption> Options { get; } =
    [
        new("Collector", ModuleIds.Collector),
    ];
}

public sealed record ModuleOption(string DisplayName, string ModuleId);
