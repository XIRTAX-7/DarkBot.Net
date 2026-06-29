using DarkBot.Net.Application.Mappers;
using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Application.BotEngine.Addresses;
using EntityInfoStub = DarkBot.Net.Core.Entities.EntityInfoStub;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Entity list from Frida bridge snapshot — port of EntityList (data only).</summary>
public sealed class EntityManager
{
    private readonly BotAddressRegistry _addresses;
    private readonly MapManager _map;
    private readonly IGameFridaProbe _frida;
    private readonly IUnityGameBridge _unityBridge;
    private readonly EntitiesApi _entitiesApi;
    private readonly IConfigApi _config;

    private ShipStub[] _ships = [];
    private NpcEntity[] _npcs = [];
    private BoxEntity[] _boxes = [];
    private MineEntity[] _mines = [];
    private PortalEntity[] _portals = [];
    private FridaEntitySnapshot[] _allSnapshots = [];
    private readonly Dictionary<int, BoxEntity> _boxCache = new();

    public EntityManager(
        BotAddressRegistry addresses,
        MapManager map,
        IGameFridaProbe frida,
        IUnityGameBridge unityBridge,
        EntitiesApi entitiesApi,
        IConfigApi config)
    {
        _addresses = addresses;
        _map = map;
        _frida = frida;
        _unityBridge = unityBridge;
        _entitiesApi = entitiesApi;
        _config = config;
        _addresses.Invalidated += OnInvalidated;
    }

    public int EntityCount => _frida.EntityCount;

    public long EntitiesArrayAddress => 0;

    public IReadOnlyList<NpcEntity> Npcs => _npcs;

    public IReadOnlyList<BoxEntity> Boxes => _boxes;

    public IReadOnlyList<PortalEntity> Portals => _portals;

    public IReadOnlyList<ShipStub> Ships => _ships;

    public IReadOnlyList<FridaEntitySnapshot> AllSnapshots => _allSnapshots;

    public void Tick()
    {
        if (!_frida.IsReady || _map.MapId < 0)
        {
            Clear();
            return;
        }

        RebuildFromSnapshot(_frida.Entities);
    }

    private void RebuildFromSnapshot(IReadOnlyList<FridaEntitySnapshot> entities)
    {
        var ships = new List<ShipStub>();
        var npcs = new List<NpcEntity>();
        var boxes = new List<BoxEntity>();
        var mines = new List<MineEntity>();
        var portals = new List<PortalEntity>();
        var allSnapshots = new List<FridaEntitySnapshot>();

        foreach (var entity in entities)
        {
            if (entity.Id <= 0 || !MapLoadValidator.IsSaneCoordinate(entity.X, entity.Y))
                continue;

            allSnapshots.Add(entity);

            var location = new MutableLocationInfo();
            location.Update(entity.X, entity.Y);

            switch (entity.Kind)
            {
                case "npc":
                    npcs.Add(new NpcEntity(_unityBridge)
                    {
                        Id = entity.Id,
                        Location = location,
                        Label = entity.Label,
                    });
                    break;
                case "box":
                    {
                        var rawLabel = entity.Label ?? string.Empty;
                        var typeName = BoxTypeNormalizer.Normalize(rawLabel);
                        var boxInfo = ResolveBoxInfo(typeName);
                        if (_boxCache.TryGetValue(entity.Id, out var cached))
                        {
                            cached.Location.Update(entity.X, entity.Y);
                            cached.RefreshFromSnapshot(typeName, rawLabel, boxInfo);
                            boxes.Add(cached);
                        }
                        else
                        {
                            var box = new BoxEntity(_unityBridge, boxInfo)
                            {
                                Id = entity.Id,
                                Location = location,
                                TypeName = typeName,
                                Hash = rawLabel,
                            };
                            _boxCache[entity.Id] = box;
                            boxes.Add(box);
                        }
                    }

                    break;
                case "portal":
                    portals.Add(new PortalEntity
                    {
                        Id = entity.Id,
                        Location = location,
                    });
                    break;
                case "mine":
                    mines.Add(new MineEntity
                    {
                        Id = entity.Id,
                        Location = location,
                    });
                    break;
                case "player":
                case "ship":
                    ships.Add(CreateShipStub(entity.Id, location, entity.IsEnemy));
                    break;
                case "pet":
                case "relay":
                case "space_ball":
                case "spaceball":
                case "static":
                case "battle_station":
                case "station_turret":
                case "base_spot":
                default:
                    ships.Add(CreateShipStub(entity.Id, location, entity.IsEnemy));
                    break;
            }
        }

        _ships = ships.ToArray();
        _npcs = npcs.ToArray();
        _boxes = boxes.ToArray();
        _mines = mines.ToArray();
        _portals = portals.ToArray();
        _allSnapshots = allSnapshots.ToArray();

        PruneBoxCache(boxes);

        _entitiesApi.ReplaceSnapshot(_ships, _npcs, _boxes, _mines, _portals);
    }

    private void PruneBoxCache(IReadOnlyCollection<BoxEntity> activeBoxes)
    {
        if (_boxCache.Count == 0)
            return;

        var activeIds = activeBoxes.Select(box => box.Id).ToHashSet();
        foreach (var id in _boxCache.Keys.Where(id => !activeIds.Contains(id)).ToArray())
            _boxCache.Remove(id);
    }

    private IBoxInfo ResolveBoxInfo(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return ResolvedBoxInfo.Disabled;

        var boxInfos = _config.CurrentDocument.Collect.BoxInfos;
        foreach (var (name, record) in boxInfos)
        {
            if (name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                return ResolvedBoxInfo.FromRecord(record);
        }

        return ResolvedBoxInfo.Disabled;
    }

    private static ShipStub CreateShipStub(int id, MutableLocationInfo location, bool isEnemy) =>
        new()
        {
            Id = id,
            EntityInfoData = new EntityInfoStub { IsEnemy = isEnemy },
            Location = location,
        };

    private void Clear()
    {
        _ships = [];
        _npcs = [];
        _boxes = [];
        _mines = [];
        _portals = [];
        _allSnapshots = [];
        _boxCache.Clear();
        _entitiesApi.ClearSnapshot();
    }

    private void OnInvalidated() => Clear();
}
