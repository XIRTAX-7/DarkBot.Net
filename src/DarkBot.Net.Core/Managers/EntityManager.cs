using DarkBot.Net.Api.Game;
using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Managers;

/// <summary>Entity list size from Frida AVM (/status).</summary>
public sealed class EntityManager
{
    private readonly BotAddressRegistry _addresses;
    private readonly MapManager _map;
    private readonly IGameFridaProbe _frida;

    private int _lastEntityCount;

    public EntityManager(BotAddressRegistry addresses, MapManager map, IGameFridaProbe frida)
    {
        _addresses = addresses;
        _map = map;
        _frida = frida;
        _addresses.Invalidated += OnInvalidated;
    }

    public int EntityCount => _lastEntityCount;

    public long EntitiesArrayAddress => 0;

    public void Tick()
    {
        if (_addresses.IsInvalid || _map.MapAddress == 0 || !_frida.IsReady)
            return;

        _lastEntityCount = _frida.EntityCount;
    }

    private void OnInvalidated() => _lastEntityCount = 0;
}
