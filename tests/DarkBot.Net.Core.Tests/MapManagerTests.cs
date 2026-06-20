using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;
using DarkBot.Net.Core.Tests.Fakes;

namespace DarkBot.Net.Core.Tests;

public class MapManagerTests
{
    [Fact]
    public void Tick_switches_map_when_id_changes()
    {
        var addresses = new BotAddressRegistry();
        var memory = new FakeGameMemoryAccess();
        var star = new StarManager();
        var hero = new HeroManager(addresses, memory, star);
        var map = new MapManager(addresses, memory, star, hero);

        const long screenManager = 0x1000;
        const long mapAddress = 0x2000;
        const long mapStatic = screenManager + 256;

        addresses.SetScreenManagerAddress(screenManager);
        memory.SetLong(mapStatic, mapAddress);
        memory.SetInt(mapAddress + 76, 21000);
        memory.SetInt(mapAddress + 80, 13500);
        memory.SetInt(mapAddress + 84, 16);

        map.Tick();

        Assert.Equal(16, map.MapId);
        Assert.Equal("Map-16", hero.Map.Name);
    }
}
