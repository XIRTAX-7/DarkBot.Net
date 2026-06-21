using DarkBot.Net.Api.Game;

namespace DarkBot.Net.Agent.Windows.Game;

public sealed class FridaGameStateProbe : IGameFridaProbe
{
    private const int MaxMapId = 1000;
    private static readonly FridaEntitySnapshot[] EmptyEntities = [];

    private readonly FridaGameApi _frida;
    private long _mapPointer;
    private long _heroPointer;
    private int _entityCount;
    private IReadOnlyList<FridaEntitySnapshot> _entities = EmptyEntities;

    public FridaGameStateProbe(FridaGameApi frida) => _frida = frida;

    public bool IsReady { get; private set; }

    public long MapPointer => _mapPointer;

    public long HeroPointer => _heroPointer;

    public int EntityCount => _entityCount;

    public IReadOnlyList<FridaEntitySnapshot> Entities => _entities;

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
            _entities = EmptyEntities;
            return;
        }

        IsReady = true;
        _mapPointer = FridaBridgeStatus.ParsePtr(status.MapAddress);
        _heroPointer = FridaBridgeStatus.ParsePtr(status.HeroStatic);
        _entityCount = status.EntityCount >= 0 && status.EntityCount < 10_000
            ? status.EntityCount
            : 0;

        if (status.Entities is { Count: > 0 })
        {
            var list = new List<FridaEntitySnapshot>(status.Entities.Count);
            foreach (var entity in status.Entities)
            {
                if (entity.Id <= 0)
                    continue;

                list.Add(new FridaEntitySnapshot(
                    entity.Id,
                    entity.X,
                    entity.Y,
                    string.IsNullOrWhiteSpace(entity.Kind) ? "unknown" : entity.Kind));
            }

            _entities = list;
            _entityCount = list.Count;
        }
        else
        {
            _entities = EmptyEntities;
        }
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

    public bool TryGetStatsSnapshot(out FridaStatsSnapshot stats)
    {
        stats = default!;

        if (!IsReady)
            return false;

        var status = _frida.CurrentStatus;
        if (status?.Ready != true)
            return false;

        var userId = status.UserId > 0 ? status.UserId : status.HeroId;
        stats = new FridaStatsSnapshot(
            userId,
            status.Credits,
            status.Uridium,
            status.Experience,
            status.Honor,
            status.Cargo,
            status.MaxCargo,
            status.NovaEnergy);

        return userId > 0 || status.Credits > 0 || status.Uridium > 0;
    }
}
