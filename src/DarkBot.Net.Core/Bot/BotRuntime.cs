using DarkBot.Net.Api.Game;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Bot;

/// <summary>Coordinates manager ticks — port of Main.validTick subset.</summary>
public sealed class BotRuntime
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameFridaProbe _frida;
    private readonly ModuleController _modules;

    public BotRuntime(
        BotAddressRegistry addresses,
        IGameFridaProbe frida,
        HeroManager hero,
        MapManager map,
        EntityManager entities,
        StatsManager stats,
        ModuleController modules)
    {
        _addresses = addresses;
        _frida = frida;
        _modules = modules;
        Hero = hero;
        Map = map;
        Entities = entities;
        Stats = stats;
    }

    public HeroManager Hero { get; }
    public MapManager Map { get; }
    public EntityManager Entities { get; }
    public StatsManager Stats { get; }

    public void Tick(bool isRunning)
    {
        if (_addresses.IsInvalid)
            return;

        _frida.Refresh();

        Stats.Tick();
        Hero.Tick();
        Map.Tick();
        Entities.Tick();
        _modules.Tick(isRunning, Hero.IsValid);
    }
}
