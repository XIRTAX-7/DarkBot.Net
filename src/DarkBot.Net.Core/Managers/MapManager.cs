using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Managers;

/// <summary>Port of MapManager — map id/dimensions from native memory.</summary>
public sealed class MapManager
{
    private const int MapAddressStaticOffset = 256;

    private readonly BotAddressRegistry _addresses;
    private readonly IGameMemoryAccess _memory;
    private readonly StarManager _starManager;
    private readonly HeroManager _hero;

    private long _mapAddressStatic;
    private long _mapAddress;

    public MapManager(
        BotAddressRegistry addresses,
        IGameMemoryAccess memory,
        StarManager starManager,
        HeroManager hero)
    {
        _addresses = addresses;
        _memory = memory;
        _starManager = starManager;
        _hero = hero;
        _addresses.ScreenManagerAddressChanged += OnScreenManagerAddressChanged;
        _addresses.Invalidated += OnInvalidated;
    }

    public int MapId { get; private set; } = -1;
    public long MapAddress => _mapAddress;
    public int InternalWidth { get; private set; } = 21000;
    public int InternalHeight { get; private set; } = 13500;
    public int TickCount { get; private set; }

    public void Tick()
    {
        if (!_addresses.HasScreenManager)
            return;

        TickCount++;
        var temp = _memory.ReadLong(_mapAddressStatic);
        if (_mapAddress != temp)
            UpdateMap(temp);
    }

    private void UpdateMap(long address)
    {
        _mapAddress = address;
        if (address == 0)
            return;

        InternalWidth = _memory.ReadInt(address + 76);
        InternalHeight = _memory.ReadInt(address + 80);
        if (InternalHeight == 13100)
            InternalHeight = 13500;
        if (InternalHeight == 26200)
            InternalHeight = 27000;

        var currMap = _memory.ReadInt(address + 84);
        if (currMap != MapId)
            SwitchMap(currMap);
    }

    private void SwitchMap(int mapId)
    {
        MapId = mapId;
        _hero.SetMap(_starManager.ById(mapId));
    }

    private void OnScreenManagerAddressChanged(long screenManagerAddress) =>
        _mapAddressStatic = screenManagerAddress + MapAddressStaticOffset;

    private void OnInvalidated()
    {
        MapId = -1;
        _mapAddress = 0;
        _hero.SetMap(_starManager.ById(-1));
    }
}
