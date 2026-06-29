using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Application.Tests.Fakes;

internal sealed class NullGameFridaProbe : IGameFridaProbe
{
    private static readonly FridaEntitySnapshot[] Empty = [];

    public bool IsReady => false;

    public long MapPointer => 0;

    public long HeroPointer => 0;

    public int EntityCount => 0;

    public IReadOnlyList<FridaEntitySnapshot> Entities => Empty;

    public IReadOnlyList<FridaBridgeZoneSnapshot> Zones => [];

    public void Refresh() { }

    public bool TryGetMapSnapshot(out int mapId, out int width, out int height)
    {
        mapId = width = height = 0;
        return false;
    }

    public bool TryGetHeroSnapshot(
        out int heroId,
        out double x,
        out double y,
        out int hp,
        out int maxHp,
        out int shield,
        out int maxShield,
        out int nano,
        out int maxNano)
    {
        heroId = hp = maxHp = shield = maxShield = nano = maxNano = 0;
        x = y = 0;
        return false;
    }

    public string? HeroShipType => null;

    public string? HeroPlayerName => null;

    public int HeroConfigId => 0;

    public FridaSelectedTargetSnapshot? SelectedTarget => null;

    public bool TryGetStatsSnapshot(out FridaStatsSnapshot stats)
    {
        stats = default;
        return false;
    }
}

internal sealed class FakeGameFridaProbe : IGameFridaProbe
{
    private static readonly FridaEntitySnapshot[] Empty = [];

    public bool IsReady { get; set; } = true;

    public long MapPointer { get; set; } = 0x5000;

    public long HeroPointer { get; set; } = 0x6000;

    public int EntityCount { get; set; }

    public IReadOnlyList<FridaEntitySnapshot> Entities { get; set; } = Empty;

    public IReadOnlyList<FridaBridgeZoneSnapshot> Zones { get; set; } = [];

    public int MapId { get; set; }

    public int MapWidth { get; set; } = 21000;

    public int MapHeight { get; set; } = 13500;

    public int HeroId { get; set; }

    public double HeroX { get; set; }

    public double HeroY { get; set; }

    public int HeroHp { get; set; } = 1000;

    public int HeroMaxHp { get; set; } = 1000;

    public int HeroShield { get; set; }

    public int HeroMaxShield { get; set; }

    public int HeroNano { get; set; }

    public int HeroMaxNano { get; set; }

    public string? HeroShipType { get; set; }

    public string? HeroPlayerName { get; set; }

    public int HeroConfigId { get; set; }

    public FridaSelectedTargetSnapshot? SelectedTarget { get; set; }

    public long Credits { get; set; }

    public void Refresh() { }

    public bool TryGetMapSnapshot(out int mapId, out int width, out int height)
    {
        mapId = MapId;
        width = MapWidth;
        height = MapHeight;

        if (!IsReady || MapPointer == 0 || mapId <= 0)
            return false;

        return width == 21000 && height is 13500 or 13100 or 26200 or 27000;
    }

    public bool TryGetHeroSnapshot(
        out int heroId,
        out double x,
        out double y,
        out int hp,
        out int maxHp,
        out int shield,
        out int maxShield,
        out int nano,
        out int maxNano)
    {
        heroId = HeroId;
        x = HeroX;
        y = HeroY;
        hp = HeroHp;
        maxHp = HeroMaxHp;
        shield = HeroShield;
        maxShield = HeroMaxShield;
        nano = HeroNano;
        maxNano = HeroMaxNano;
        return IsReady && heroId > 0;
    }

    public bool TryGetStatsSnapshot(out FridaStatsSnapshot stats)
    {
        stats = new FridaStatsSnapshot(HeroId, Credits, 0, 0, 0, 0, 0, 0);
        return IsReady && (HeroId > 0 || Credits > 0);
    }
}
