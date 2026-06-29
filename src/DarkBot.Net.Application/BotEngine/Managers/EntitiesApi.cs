using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.BotEngine.Managers;

public sealed class EntitiesApi : IEntitiesApi
{
    private readonly List<IShip> _ships = [];
    private readonly List<INpc> _npcs = [];
    private readonly List<IBox> _boxes = [];
    private readonly List<IMine> _mines = [];
    private readonly List<IPortal> _portals = [];
    private readonly List<IEntity> _all = [];

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
        _ships.Clear();
        _ships.AddRange(ships);

        _npcs.Clear();
        _npcs.AddRange(npcs);

        _boxes.Clear();
        _boxes.AddRange(boxes);

        _mines.Clear();
        _mines.AddRange(mines);

        _portals.Clear();
        _portals.AddRange(portals);

        _all.Clear();
        _all.AddRange(_ships.Cast<IEntity>());
        _all.AddRange(_npcs);
        _all.AddRange(_boxes);
        _all.AddRange(_mines);
        _all.AddRange(_portals);
    }

    public void ClearSnapshot() => ReplaceSnapshot([], [], [], [], []);
}
