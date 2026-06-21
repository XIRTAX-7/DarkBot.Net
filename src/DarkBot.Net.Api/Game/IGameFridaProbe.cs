namespace DarkBot.Net.Api.Game;

/// <summary>Game state from darkDev Frida AVM (/status) — no DarkMem reads.</summary>
public interface IGameFridaProbe
{
    void Refresh();

    bool IsReady { get; }

    long MapPointer { get; }

    long HeroPointer { get; }

    int EntityCount { get; }

    bool TryGetMapSnapshot(out int mapId, out int width, out int height);

    bool TryGetHeroSnapshot(out int heroId, out double x, out double y, out int hp, out int maxHp);
}
