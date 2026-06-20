using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;
using DarkBot.Net.Core.Tests.Fakes;

namespace DarkBot.Net.Core.Tests;

public class HeroManagerTests
{
    [Fact]
    public void Tick_reads_hero_hp_from_memory()
    {
        var addresses = new BotAddressRegistry();
        var memory = new FakeGameMemoryAccess();
        var hero = new HeroManager(addresses, memory, new StarManager());

        const long screenManager = 0x1000;
        const long heroStatic = screenManager + 240;
        const long shipAddress = 0x3000;

        addresses.SetScreenManagerAddress(screenManager);
        memory.SetLong(heroStatic, shipAddress);
        memory.SetInt(shipAddress + 56, 42);
        memory.SetHeroHp(shipAddress, 180000);

        hero.Tick();

        Assert.True(hero.IsValid);
        Assert.Equal(42, hero.Id);
        Assert.Equal(180000, hero.Health.Hp);
    }
}
