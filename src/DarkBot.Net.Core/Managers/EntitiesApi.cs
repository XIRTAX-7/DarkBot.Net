using DarkBot.Net.Api.Game.Entities;
using DarkBot.Net.Api.Managers;

namespace DarkBot.Net.Core.Managers;

public sealed class EntitiesApi : IEntitiesApi
{
    private readonly List<ShipStub> _ships = [];
    private readonly List<StationStub> _stations = [];

    public IReadOnlyCollection<INpc> Npcs => [];
    public IReadOnlyCollection<IPet> Pets => [];
    public IReadOnlyCollection<IPlayer> Players => [];
    public IReadOnlyCollection<IShip> Ships => _ships;
    public IReadOnlyCollection<IBox> Boxes => [];
    public IReadOnlyCollection<IMine> Mines => [];
    public IReadOnlyCollection<IPortal> Portals => [];
    public IReadOnlyCollection<IEntity> All => _ships.Cast<IEntity>().Concat(_stations).ToList();

    public void AddShip(ShipStub ship) => _ships.Add(ship);
    public void AddStation(StationStub station) => _stations.Add(station);
    public void ClearShips() => _ships.Clear();
}
