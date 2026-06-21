using DarkBot.Net.Api.Game.Stats;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Bot;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Ui.Services;

public sealed class BotUiStateService
{
    private readonly HeroManager _hero;
    private readonly MapManager _map;
    private readonly IStatsApi _stats;
    private readonly IBotController _bot;
    private readonly IBackpageApi _backpage;

    public BotUiStateService(
        HeroManager hero,
        MapManager map,
        IStatsApi stats,
        IBotController bot,
        IBackpageApi backpage)
    {
        _hero = hero;
        _map = map;
        _stats = stats;
        _bot = bot;
        _backpage = backpage;
    }

    public BotUiSnapshot Capture()
    {
        var health = _hero.Health;
        return new BotUiSnapshot(
            HeroValid: _hero.IsValid,
            HeroOnMap: _hero.HasMapPosition,
            HeroId: _hero.ShipId,
            HeroX: _hero.X,
            HeroY: _hero.Y,
            HeroHp: health.Hp,
            HeroMaxHp: health.MaxHp,
            MapId: _map.MapId,
            MapName: _hero.Map.Name,
            MapWidth: _map.InternalWidth,
            MapHeight: _map.InternalHeight,
            Portals: _map.Portals
                .Select(p => new MapPortalSnapshot(p.X, p.Y, p.TargetShortName))
                .ToArray(),
            BotRunning: _bot.IsRunning,
            TickCount: _bot.TickCount,
            LastTickMs: _bot.LastTickMs,
            Credits: _stats.GetStatValue(Stats.General.Credits),
            Uridium: _stats.GetStatValue(Stats.General.Uridium),
            Experience: _stats.GetStatValue(Stats.General.Experience),
            Honor: _stats.GetStatValue(Stats.General.Honor),
            Ping: _stats.Ping,
            BackpageStatus: _backpage.SidStatus,
            BackpageValid: _backpage.IsInstanceValid());
    }
}
