using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Application.BotEngine.Addresses;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Port of MapManager — map state from Frida AVM (/status).</summary>
public sealed class MapManager
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameFridaProbe _frida;
    private readonly StarManager _starManager;
    private readonly HeroManager _hero;
    private readonly ILogger<MapManager> _logger;

    private long _mapAddress;

    public MapManager(
        BotAddressRegistry addresses,
        IGameFridaProbe frida,
        StarManager starManager,
        HeroManager hero,
        ILogger<MapManager> logger)
    {
        _addresses = addresses;
        _frida = frida;
        _starManager = starManager;
        _hero = hero;
        _logger = logger;
        _addresses.Invalidated += OnInvalidated;
    }

    public int MapId { get; private set; } = -1;
    public long MapAddress => _mapAddress;
    public int InternalWidth { get; private set; } = 21000;
    public int InternalHeight { get; private set; } = 13500;
    public int TickCount { get; private set; }

    public IReadOnlyList<MapPortalInfo> Portals { get; private set; } = Array.Empty<MapPortalInfo>();

    public void Tick()
    {
        if (!_addresses.HasScreenManager || !_frida.IsReady)
            return;

        TickCount++;

        if (!TryApplyFridaMap())
            return;
    }

    private bool TryApplyFridaMap()
    {
        if (!_frida.TryGetMapSnapshot(out var currMap, out var width, out var height))
            return false;

        var ptr = _frida.MapPointer;
        if (ptr != 0)
            _mapAddress = ptr;

        ApplyDimensions(width, height);

        if (currMap == MapId)
            return true;

        SwitchMap(currMap);
        return true;
    }

    private void ApplyDimensions(int width, int height)
    {
        InternalWidth = width;

        if (height == 13100)
            height = 13500;
        else if (height == 26200)
            height = 27000;

        InternalHeight = height;
    }

    private void SwitchMap(int mapId)
    {
        MapId = mapId;
        var map = _starManager.ById(mapId);
        Portals = _starManager.GetPortals(mapId);
        _hero.SetMap(map);
        _logger.LogInformation(
            "Map switched to {MapId} ({MapName}), {PortalCount} portals",
            mapId,
            map.Name,
            Portals.Count);
    }

    private void ResetToLoading()
    {
        _mapAddress = 0;
        Portals = Array.Empty<MapPortalInfo>();

        if (MapId == -1)
            return;

        MapId = -1;
        _hero.SetMap(_starManager.ById(-1));
        _logger.LogDebug("Map reset to loading");
    }

    private void OnInvalidated() => ResetToLoading();
}
