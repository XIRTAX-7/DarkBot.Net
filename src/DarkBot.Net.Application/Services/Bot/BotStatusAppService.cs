using DarkBot.Net.Application.BotEngine.Addresses;
using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Application.Mappers.Bot;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Services.Bot;

/// <summary>Фасад снимка состояния бота для Presentation.</summary>
public sealed class BotStatusAppService(
    BotAddressRegistry addresses,
    HeroManager hero,
    MapManager map,
    EntityManager entities,
    IGameFridaProbe frida,
    IStatsApi stats,
    IBotController bot,
    IMovementApi movement) : IBotStatusAppService
{
    public BotStatusSnapshot Capture()
    {
        if (!addresses.IsInvalid)
        {
            frida.Refresh();
            hero.Tick();
            map.Tick();
        }

        return BotStatusSnapshotMapper.Create(hero, map, entities, frida, stats, bot, movement);
    }
}
