using DarkBot.Net.Core.Interfaces.Bot;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Application.Mappers.Bot;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Services.Bot;

/// <summary>Фасад снимка состояния бота для Presentation.</summary>
public sealed class BotStatusAppService(
    HeroManager hero,
    MapManager map,
    EntityManager entities,
    StatsManager stats,
    IGameFridaProbe frida,
    IBotController bot,
    IMovementApi movement) : IBotStatusAppService
{
    public BotStatusSnapshot Capture()
    {
        frida.Refresh();

        if (frida.IsReady)
        {
            hero.Tick();
            map.Tick();
            entities.Tick();
            stats.Tick(bot.IsRunning);
        }

        return BotStatusSnapshotMapper.Create(hero, map, entities, frida, stats, bot, movement);
    }
}
