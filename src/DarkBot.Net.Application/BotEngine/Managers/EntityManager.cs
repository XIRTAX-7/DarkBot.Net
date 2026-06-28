using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Entities;
using EntityInfoStub = DarkBot.Net.Core.Entities.EntityInfoStub;
using DarkBot.Net.Application.BotEngine.Addresses;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Entity list from Frida bridge snapshot — port of EntityList (data only).</summary>
public sealed class EntityManager
{
    private readonly BotAddressRegistry _addresses;
    private readonly MapManager _map;
    private readonly IGameFridaProbe _frida;
    private readonly EntitiesApi _entitiesApi;

    private readonly List<ShipStub> _ships = [];
    private readonly List<ShipStub> _npcs = [];
    private readonly List<ShipStub> _boxes = [];
    private readonly List<ShipStub> _portals = [];
    private readonly List<FridaEntitySnapshot> _allSnapshots = [];

    public EntityManager(
        BotAddressRegistry addresses,
        MapManager map,
        IGameFridaProbe frida,
        EntitiesApi entitiesApi)
    {
        _addresses = addresses;
        _map = map;
        _frida = frida;
        _entitiesApi = entitiesApi;
        _addresses.Invalidated += OnInvalidated;
    }

    public int EntityCount => _frida.EntityCount;

    public long EntitiesArrayAddress => 0;

    public IReadOnlyList<ShipStub> Npcs => _npcs;

    public IReadOnlyList<ShipStub> Boxes => _boxes;

    public IReadOnlyList<ShipStub> Portals => _portals;

    public IReadOnlyList<ShipStub> Ships => _ships;

    public IReadOnlyList<FridaEntitySnapshot> AllSnapshots => _allSnapshots;

    public void Tick()
    {
        if (_addresses.IsInvalid || _map.MapAddress == 0 || !_frida.IsReady)
        {
            Clear();
            return;
        }

        _frida.Refresh();
        RebuildFromSnapshot(_frida.Entities);
    }

    private void RebuildFromSnapshot(IReadOnlyList<FridaEntitySnapshot> entities)
    {
        _ships.Clear();
        _npcs.Clear();
        _boxes.Clear();
        _portals.Clear();
        _allSnapshots.Clear();
        _entitiesApi.ClearShips();

        foreach (var entity in entities)
        {
            if (entity.Id <= 0 || !MapLoadValidator.IsSaneCoordinate(entity.X, entity.Y))
                continue;

            _allSnapshots.Add(entity);

            var location = new MutableLocationInfo();
            location.Update(entity.X, entity.Y);

            var stub = new ShipStub
            {
                Id = entity.Id,
                EntityInfoData = new EntityInfoStub(),
                Location = location
            };

            switch (entity.Kind)
            {
                case "npc":
                    _npcs.Add(stub);
                    break;
                case "box":
                    _boxes.Add(stub);
                    break;
                case "portal":
                    _portals.Add(stub);
                    break;
                case "player":
                case "ship":
                    _ships.Add(stub);
                    _entitiesApi.AddShip(stub);
                    break;
                case "mine":
                case "pet":
                case "relay":
                case "space_ball":
                case "spaceball":
                case "static":
                case "battle_station":
                case "station_turret":
                case "base_spot":
                    _ships.Add(stub);
                    break;
                default:
                    _ships.Add(stub);
                    break;
            }
        }
    }

    private void Clear()
    {
        _ships.Clear();
        _npcs.Clear();
        _boxes.Clear();
        _portals.Clear();
        _allSnapshots.Clear();
        _entitiesApi.ClearShips();
    }

    private void OnInvalidated() => Clear();
}
