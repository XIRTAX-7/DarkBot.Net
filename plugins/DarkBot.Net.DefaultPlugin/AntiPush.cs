using DarkBot.Net.Api.Config;
using DarkBot.Net.Api.Game.Entities;
using DarkBot.Net.Api.Game.Enums;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.DefaultPlugin;

[Feature("Anti push", "Turns off the bot if an enemy uses draw fire or is killed over X times by the same player", EnabledByDefault = true)]
public sealed class AntiPush : IBehavior, IConfigurable<AntiPush.Config>
{
    private readonly IHeroApi _hero;
    private readonly IBotApi _bot;
    private readonly IRepairApi _repair;
    private readonly II18nApi _i18n;
    private readonly ILegacyModuleApi _legacyModules;
    private readonly IReadOnlyCollection<IShip> _ships;

    private Config _config = new();
    private readonly Dictionary<int, List<DateTimeOffset>> _deathStats = new();
    private bool _wasDead = true;

    public AntiPush(
        IHeroApi hero,
        IBotApi bot,
        IRepairApi repair,
        II18nApi i18n,
        ILegacyModuleApi legacyModules,
        IEntitiesApi entities)
    {
        _hero = hero;
        _bot = bot;
        _repair = repair;
        _i18n = i18n;
        _legacyModules = legacyModules;
        _ships = entities.Ships;
    }

    public void SetConfig(IConfigSetting<Config> config) => _config = config.Value;

    public void OnTickBehavior()
    {
        TickDrawFire();
        TickDeathPause();
    }

    public void OnStoppedBehavior()
    {
        if (_config.DeathPauseTime == 0)
            return;

        RemoveOldDeaths();
        if (_repair.IsDestroyed && !_wasDead)
        {
            var killer = _ships.FirstOrDefault(s =>
                s.EntityInfo.Username == _repair.LastDestroyerName);
            if (killer is not null)
            {
                if (!_deathStats.TryGetValue(killer.Id, out var deaths))
                    _deathStats[killer.Id] = deaths = [];

                deaths.Add(DateTimeOffset.UtcNow);
            }

            _wasDead = true;
        }
    }

    private void TickDrawFire()
    {
        if (_config.DrawfirePauseTime == 0)
            return;

        foreach (var ship in _ships)
        {
            if (!ship.EntityInfo.IsEnemy || !ship.HasEffect(EntityEffect.DrawFire))
                continue;

            if (!ReferenceEquals(_hero.Target, ship))
                continue;

            var pauseMillis = _config.DrawfirePauseTime > 0
                ? (long)_config.DrawfirePauseTime * 60 * 1000
                : (long?)null;

            _bot.SetModule(_legacyModules.GetDisconnectModule(
                pauseMillis,
                _i18n.Get("module.disconnect.reason.draw_fire")));
            _bot.SetRunning(false);
            return;
        }
    }

    private void TickDeathPause()
    {
        if (_config.DeathPauseTime == 0)
            return;

        _wasDead = false;

        foreach (var entry in _deathStats)
        {
            if (entry.Value.Count < _config.MaxDeaths)
                continue;

            var killer = _repair.LastDestroyerName ?? "Unknown";
            _bot.SetModule(_legacyModules.GetDisconnectModule(
                _config.DeathPauseTime > 0 ? _config.DeathPauseTime * 60 * 1000L : null,
                _i18n.Get("module.disconnect.reason.death_pause", killer, entry.Value.Count)));
            _bot.SetRunning(false);
            _deathStats.Remove(entry.Key);
            break;
        }
    }

    private void RemoveOldDeaths()
    {
        foreach (var times in _deathStats.Values)
            times.RemoveAll(t => DateTimeOffset.UtcNow - t >= TimeSpan.FromDays(1));

        foreach (var key in _deathStats.Where(e => e.Value.Count == 0).Select(e => e.Key).ToList())
            _deathStats.Remove(key);
    }

    public sealed class Config
    {
        public int DrawfirePauseTime { get; set; } = -1;
        public int MaxDeaths { get; set; } = 7;
        public int DeathPauseTime { get; set; } = -1;
    }
}
