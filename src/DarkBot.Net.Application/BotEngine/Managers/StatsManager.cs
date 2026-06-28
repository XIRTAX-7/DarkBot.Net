using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Stats;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Application.BotEngine.Addresses;
using StatImpl = DarkBot.Net.Application.BotEngine.Statistics.StatImpl;
using AverageStatImpl = DarkBot.Net.Application.BotEngine.Statistics.AverageStatImpl;
using StatKey = DarkBot.Net.Application.BotEngine.Statistics.StatKey;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Port of StatsManager — session stats from Frida bridge snapshot.</summary>
public sealed class StatsManager : IStatsApi, ISessionMetadataProvider
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameFridaProbe _frida;
    private readonly Dictionary<StatKey, StatImpl> _statistics = new();
    private readonly StatImpl _runtime;
    private readonly StatImpl _credits;
    private readonly StatImpl _uridium;
    private readonly StatImpl _experience;
    private readonly StatImpl _honor;
    private readonly StatImpl _cargo;
    private readonly StatImpl _maxCargo;
    private readonly StatImpl _novaEnergy;
    private readonly StatImpl _teleportBonus;
    private readonly AverageStatImpl _pingStat;
    private readonly AverageStatImpl _tickStat;
    private readonly AverageStatImpl _memoryStat;
    private readonly AverageStatImpl _cpuStat;

    private bool _updateStats = true;
    private bool _botRunning;

    public StatsManager(BotAddressRegistry addresses, IGameFridaProbe frida)
    {
        _addresses = addresses;
        _frida = frida;

        Register(Stats.Bot.Runtime, _runtime = CreateStat());
        Register(Stats.General.Credits, _credits = CreateStat());
        Register(Stats.General.Uridium, _uridium = CreateStat());
        Register(Stats.General.Experience, _experience = CreateStat());
        Register(Stats.General.Honor, _honor = CreateStat());
        Register(Stats.General.Cargo, _cargo = CreateStat());
        Register(Stats.General.MaxCargo, _maxCargo = CreateStat());
        Register(Stats.General.NovaEnergy, _novaEnergy = CreateStat());
        Register(Stats.General.TeleportBonusAmount, _teleportBonus = CreateStat());
        Register(Stats.Bot.Ping, _pingStat = new AverageStatImpl());
        Register(Stats.Bot.TickTime, _tickStat = new AverageStatImpl());
        Register(Stats.Bot.Memory, _memoryStat = new AverageStatImpl());
        Register(Stats.Bot.Cpu, _cpuStat = new AverageStatImpl());

        _addresses.Invalidated += () =>
        {
            UserId = 0;
            Sid = null;
        };
    }

    public string? Sid { get; private set; }
    public int UserId { get; private set; }
    public string? Instance { get; private set; }
    public bool IsPremium { get; private set; }

    public string? InstanceUrl => Instance;

    public void SetSessionMetadata(string? sid, int userId, string? instance)
    {
        Sid = sid;
        UserId = userId;
        Instance = instance;
    }

    public void UpdateSession(string sid, int userId, string instanceUrl) =>
        SetSessionMetadata(sid, userId, instanceUrl);

    public void SetUpdateStatsWhilePaused(bool enabled) => _updateStats = enabled;

    public void Tick(bool botRunning)
    {
        _botRunning = botRunning;
        UpdateNonZero(_runtime, Environment.TickCount64);

        if (!_frida.IsReady || !_frida.TryGetStatsSnapshot(out var stats))
            return;

        if (stats.UserId > 0)
            UserId = stats.UserId;

        UpdateNonZero(_credits, stats.Credits);
        UpdateNonZero(_uridium, stats.Uridium);
        UpdateNonZero(_experience, stats.Experience);
        UpdateNonZero(_honor, stats.Honor);

        if (stats.Cargo >= 0)
            _cargo.Track(stats.Cargo);
        if (stats.MaxCargo > 0)
            _maxCargo.Track(stats.MaxCargo);
        if (stats.NovaEnergy >= 0)
            _novaEnergy.Track(stats.NovaEnergy);
    }

    public void TickAverageStats(double tickTimeMs, int ping = 0)
    {
        if (ping > 0)
            _pingStat.Track(ping);

        _tickStat.Track(tickTimeMs);
    }

    public IStatsApi.IStat GetStat(IStatsApi.IStatKey key) =>
        _statistics[StatKey.From(key)];

    public IStatsApi.IStat RegisterStat(IStatsApi.IStatKey key)
    {
        if (key.Namespace is null)
            throw new NotSupportedException("Custom stat keys require a namespace.");

        return _statistics.TryGetValue(StatKey.From(key), out var existing)
            ? existing
            : _statistics[StatKey.From(key)] = CreateStat();
    }

    public void SetStatValue(IStatsApi.IStatKey key, double newValue)
    {
        if (key.Namespace is null)
            throw new NotSupportedException("Custom stat keys require a namespace.");

        _statistics[StatKey.From(key)].Track(newValue);
    }

    public void ResetStats()
    {
        foreach (var stat in _statistics.Values)
            stat.Reset();
    }

    private StatImpl CreateStat() => new(ShouldTrackSessionStats);

    private bool ShouldTrackSessionStats() => _updateStats && _botRunning;

    private void Register(IStatsApi.IStatKey key, StatImpl stat) =>
        _statistics[StatKey.From(key)] = stat;

    private double UpdateNonZero(StatImpl stat, double value)
    {
        if (value == 0)
            return 0;

        return stat.Track(value);
    }
}
