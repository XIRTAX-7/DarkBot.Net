using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

public sealed class UnityBridgeStateProbe : IGameFridaProbe
{
    private const int MaxMapId = 1000;
    private static readonly FridaEntitySnapshot[] EmptyEntities = [];
    private static readonly FridaBridgeZoneSnapshot[] EmptyZones = [];

    private readonly IGameBridgeStatusSource _bridge;
    private long _mapPointer;
    private long _heroPointer;
    private int _entityCount;
    private IReadOnlyList<FridaEntitySnapshot> _entities = EmptyEntities;
    private IReadOnlyList<FridaBridgeZoneSnapshot> _zones = EmptyZones;

    public UnityBridgeStateProbe(IGameBridgeStatusSource bridge) => _bridge = bridge;

    public bool IsReady { get; private set; }

    public long MapPointer => _mapPointer;

    public long HeroPointer => _heroPointer;

    public int EntityCount => _entityCount;

    public IReadOnlyList<FridaEntitySnapshot> Entities => _entities;

    public IReadOnlyList<FridaBridgeZoneSnapshot> Zones => _zones;

    public string? HeroShipType { get; private set; }

    public string? HeroPlayerName { get; private set; }

    public int HeroConfigId { get; private set; }

    public void Refresh()
    {
        if (!_bridge.RefreshStatus())
            return;

        var status = _bridge.CurrentStatus;
        if (status?.Ready != true)
        {
            IsReady = false;
            _mapPointer = 0;
            _heroPointer = 0;
            _entityCount = 0;
            _entities = EmptyEntities;
            _zones = EmptyZones;
            HeroShipType = null;
            HeroPlayerName = null;
            HeroConfigId = 0;
            return;
        }

        IsReady = true;
        _mapPointer = FridaBridgeStatus.ParsePtr(status.MapAddress);
        _heroPointer = FridaBridgeStatus.ParsePtr(status.HeroStatic);
        HeroShipType = status.HeroShipType;
        HeroPlayerName = status.HeroPlayerName;
        HeroConfigId = status.HeroConfigId is 1 or 2 ? status.HeroConfigId : 0;
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
                    string.IsNullOrWhiteSpace(entity.Kind) ? "unknown" : entity.Kind,
                    entity.Fill,
                    entity.Label,
                    entity.IsEnemy,
                    entity.IsGroupMember,
                    entity.SubKind));
            }

            _entities = list;
            _entityCount = list.Count;
        }
        else
        {
            _entities = EmptyEntities;
        }

        if (status.Zones is { Count: > 0 })
        {
            var zoneList = new List<FridaBridgeZoneSnapshot>(status.Zones.Count);
            foreach (var zone in status.Zones)
            {
                if (zone.Polygon is not { Count: > 0 } polygon)
                    continue;

                zoneList.Add(new FridaBridgeZoneSnapshot(
                    string.IsNullOrWhiteSpace(zone.Kind) ? "mist" : zone.Kind,
                    polygon.Select(p => new MapPointSnapshotCore(p.X, p.Y)).ToArray()));
            }

            _zones = zoneList;
        }
        else
        {
            _zones = EmptyZones;
        }
    }

    public bool TryGetMapSnapshot(out int mapId, out int width, out int height)
    {
        mapId = width = height = 0;

        if (!IsReady || _mapPointer == 0)
            return false;

        var status = _bridge.CurrentStatus;
        if (status?.Ready != true)
            return false;

        mapId = status.MapId;
        width = status.MapWidth;
        height = status.MapHeight;

        if (mapId is <= 0 or >= MaxMapId)
            return false;

        return width > 0 && height > 0;
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

        if (!IsReady)
            return false;

        var status = _bridge.CurrentStatus;
        if (status?.Ready != true || status.HeroId <= 0)
            return false;

        heroId = status.HeroId;
        x = status.HeroX;
        y = status.HeroY;
        hp = status.HeroHp;
        maxHp = status.HeroMaxHp;
        shield = status.HeroShield;
        maxShield = status.HeroMaxShield;
        nano = status.HeroNano;
        maxNano = status.HeroMaxNano;
        return true;
    }

    public bool TryGetStatsSnapshot(out FridaStatsSnapshot stats)
    {
        stats = default!;

        if (!IsReady)
            return false;

        var status = _bridge.CurrentStatus;
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

        return userId > 0
            || status.Credits > 0
            || status.Uridium > 0
            || status.MaxCargo > 0
            || status.Cargo > 0;
    }
}
