using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Managers;

/// <summary>
/// Port of EntityManager — reads entity list from map memory when addresses are installed.
/// Full entity typing (NPC/box/portal) follows Java factory rules in later iterations.
/// </summary>
public sealed class EntityManager
{
    private readonly BotAddressRegistry _addresses;
    private readonly MapManager _map;
    private readonly IGameConnection _game;

    private long _entitiesArrayAddress;
    private int _lastEntityCount;

    public EntityManager(BotAddressRegistry addresses, MapManager map, IGameConnection game)
    {
        _addresses = addresses;
        _map = map;
        _game = game;
        _addresses.Invalidated += OnInvalidated;
    }

    public int EntityCount => _lastEntityCount;

    public long EntitiesArrayAddress => _entitiesArrayAddress;

    public void Tick()
    {
        if (_addresses.IsInvalid || _map.MapAddress == 0)
            return;

        var arrayHeader = _game.ReadLong(_map.MapAddress + 40);
        if (arrayHeader == 0)
            return;

        _entitiesArrayAddress = arrayHeader;
        var size = _game.ReadInt(arrayHeader + 0x18);
        if (size >= 0 && size < 10_000)
            _lastEntityCount = size;
    }

    private void OnInvalidated()
    {
        _entitiesArrayAddress = 0;
        _lastEntityCount = 0;
    }
}
