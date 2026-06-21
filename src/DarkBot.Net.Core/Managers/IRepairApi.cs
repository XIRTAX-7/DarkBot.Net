namespace DarkBot.Net.Core.Managers;

/// <summary>Port of eu.darkbot.api.managers.RepairAPI (subset for Phase 4).</summary>
public interface IRepairApi : IApi.ISingleton
{
    bool IsDestroyed { get; }
    string? LastDestroyerName { get; }
}
