using DarkBot.Net.Core.Game.Stats;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Application.Bot;
using DarkBot.Net.Application.Managers;

namespace DarkBot.Net.Presentation.Services;

public sealed class BotUiStateService(
    HeroManager hero,
    MapManager map,
    IStatsApi stats,
    IBotController bot)
{
    public BotUiSnapshot Capture()
    {
        var health = hero.Health;
        return new BotUiSnapshot(
            HeroValid: hero.IsValid,
            HeroOnMap: hero.HasMapPosition,
            HeroId: hero.ShipId,
            HeroX: hero.X,
            HeroY: hero.Y,
            HeroHp: health.Hp,
            HeroMaxHp: health.MaxHp,
            MapId: map.MapId,
            MapName: hero.Map.Name,
            MapWidth: map.InternalWidth,
            MapHeight: map.InternalHeight,
            Portals: map.Portals
                .Select(p => new MapPortalSnapshot(p.X, p.Y, p.TargetShortName))
                .ToArray(),
            BotRunning: bot.IsRunning,
            TickCount: bot.TickCount,
            LastTickMs: bot.LastTickMs,
            Credits: stats.GetStatValue(Stats.General.Credits),
            Uridium: stats.GetStatValue(Stats.General.Uridium),
            Experience: stats.GetStatValue(Stats.General.Experience),
            Honor: stats.GetStatValue(Stats.General.Honor),
            Ping: stats.Ping);
    }
}
