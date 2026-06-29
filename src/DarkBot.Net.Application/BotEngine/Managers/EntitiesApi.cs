using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.BotEngine.Managers;

public sealed class EntitiesApi : IEntitiesApi
{
    private IShip[] _ships = [];
    private INpc[] _npcs = [];
    private IBox[] _boxes = [];
    private IMine[] _mines = [];
    private IPortal[] _portals = [];
    private IEntity[] _all = [];

    public IReadOnlyCollection<INpc> Npcs => _npcs;
    public IReadOnlyCollection<IPet> Pets => [];
    public IReadOnlyCollection<IPlayer> Players => [];
    public IReadOnlyCollection<IShip> Ships => _ships;
    public IReadOnlyCollection<IBox> Boxes => _boxes;
    public IReadOnlyCollection<IMine> Mines => _mines;
    public IReadOnlyCollection<IPortal> Portals => _portals;
    public IReadOnlyCollection<IEntity> All => _all;

    public void ReplaceSnapshot(
        IEnumerable<IShip> ships,
        IEnumerable<INpc> npcs,
        IEnumerable<IBox> boxes,
        IEnumerable<IMine> mines,
        IEnumerable<IPortal> portals)
    {
        var nextShips = ships.ToArray();
        var nextNpcs = npcs.ToArray();
        var nextBoxes = boxes.ToArray();
        var nextMines = mines.ToArray();
        var nextPortals = portals.ToArray();

        _ships = nextShips;
        _npcs = nextNpcs;
        _boxes = nextBoxes;
        _mines = nextMines;
        _portals = nextPortals;
        _all = BuildAllSnapshot(nextShips, nextNpcs, nextBoxes, nextMines, nextPortals);
    }

    private static IEntity[] BuildAllSnapshot(
        IShip[] ships,
        INpc[] npcs,
        IBox[] boxes,
        IMine[] mines,
        IPortal[] portals)
    {
        var all = new List<IEntity>(ships.Length + npcs.Length + boxes.Length + mines.Length + portals.Length);
        all.AddRange(ships);
        all.AddRange(npcs);
        all.AddRange(boxes);
        all.AddRange(mines);
        all.AddRange(portals);
        return all.ToArray();
    }

    public void ClearSnapshot() => ReplaceSnapshot([], [], [], [], []);
}
