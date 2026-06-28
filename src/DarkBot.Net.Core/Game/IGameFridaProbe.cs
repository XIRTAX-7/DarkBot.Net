namespace DarkBot.Net.Core.Game;

/// <summary>Game state from darkDev Frida AVM (/status + WS) — Frida-only, no external memory reads.</summary>
public interface IGameFridaProbe
{
    void Refresh();

    bool IsReady { get; }

    long MapPointer { get; }

    long HeroPointer { get; }

    int EntityCount { get; }

    IReadOnlyList<FridaEntitySnapshot> Entities { get; }

    IReadOnlyList<FridaBridgeZoneSnapshot> Zones { get; }

    bool TryGetMapSnapshot(out int mapId, out int width, out int height);

    bool TryGetHeroSnapshot(
        out int heroId,
        out double x,
        out double y,
        out int hp,
        out int maxHp,
        out int shield,
        out int maxShield,
        out int nano,
        out int maxNano);

    string? HeroShipType { get; }

    string? HeroPlayerName { get; }

    int HeroConfigId { get; }

    bool TryGetStatsSnapshot(out FridaStatsSnapshot stats);
}
