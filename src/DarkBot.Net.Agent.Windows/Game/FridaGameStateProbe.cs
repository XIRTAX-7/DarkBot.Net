using DarkBot.Net.Api.Game;

namespace DarkBot.Net.Agent.Windows.Game;

public sealed class FridaGameStateProbe : IGameFridaProbe
{
    private const int MaxMapId = 1000;

    private readonly FridaGameApi _frida;
    private long _mapPointer;
    private long _heroPointer;
    private int _entityCount;

    public FridaGameStateProbe(FridaGameApi frida) => _frida = frida;

    public bool IsReady { get; private set; }

    public long MapPointer => _mapPointer;

    public long HeroPointer => _heroPointer;

    public int EntityCount => _entityCount;

    public void Refresh()
    {
        if (!_frida.RefreshStatus())
            return;

        var status = _frida.CurrentStatus;
        if (status?.Ready != true)
        {
            IsReady = false;
            _mapPointer = 0;
            _heroPointer = 0;
            _entityCount = 0;
            return;
        }

        IsReady = true;
        _mapPointer = FridaBridgeStatus.ParsePtr(status.MapAddress);
        _heroPointer = FridaBridgeStatus.ParsePtr(status.HeroStatic);
        _entityCount = status.EntityCount >= 0 && status.EntityCount < 10_000
            ? status.EntityCount
            : 0;
    }

    public bool TryGetMapSnapshot(out int mapId, out int width, out int height)
    {
        mapId = width = height = 0;

        if (!IsReady || _mapPointer == 0)
            return false;

        var status = _frida.CurrentStatus;
        if (status?.Ready != true)
            return false;

        mapId = status.MapId;
        width = status.MapWidth;
        height = status.MapHeight;

        // Java MapManager.update — reject only obvious garbage, no fixed width check.
        if (mapId is <= 0 or >= MaxMapId)
            return false;

        return width > 0 && height > 0;
    }

    public bool TryGetHeroSnapshot(out int heroId, out double x, out double y, out int hp, out int maxHp)
    {
        heroId = hp = maxHp = 0;
        x = y = 0;

        if (!IsReady)
            return false;

        var status = _frida.CurrentStatus;
        if (status?.Ready != true || status.HeroId <= 0)
            return false;

        heroId = status.HeroId;
        x = status.HeroX;
        y = status.HeroY;
        hp = status.HeroHp;
        maxHp = status.HeroMaxHp;
        return true;
    }
}
