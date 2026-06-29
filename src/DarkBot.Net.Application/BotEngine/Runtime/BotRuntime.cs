using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Application.BotEngine.Runtime;

/// <summary>Coordinates manager ticks — port of Main.validTick subset.</summary>
public sealed class BotRuntime
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameFridaProbe _frida;
    private readonly BotModuleRunner _modules;

    public BotRuntime(
        BotAddressRegistry addresses,
        IGameFridaProbe frida,
        HeroManager hero,
        MapManager map,
        EntityManager entities,
        StatsManager stats,
        BotModuleRunner modules)
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
        _frida.Refresh();

        if (_frida.IsReady)
        {
            Stats.Tick(isRunning);
            Hero.Tick();
            Map.Tick();
            Entities.Tick();
        }

        if (_addresses.IsInvalid)
            return;

        _modules.Tick(isRunning, Hero.IsValid);
    }
}
