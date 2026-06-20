using DarkBot.Net.Agent.Windows.Memory;
using DarkBot.Net.Api.Game.Stats;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Memory;
using StatImpl = DarkBot.Net.Core.Statistics.StatImpl;
using AverageStatImpl = DarkBot.Net.Core.Statistics.AverageStatImpl;
using StatKey = DarkBot.Net.Core.Statistics.StatKey;

namespace DarkBot.Net.Core.Managers;

/// <summary>Port of StatsManager — session stats from heroInfo memory block.</summary>
public sealed class StatsManager : IStatsApi, ISessionMetadataProvider
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameMemoryAccess _memory;
    private readonly GameMemoryReader _reader;
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

    public StatsManager(BotAddressRegistry addresses, IGameMemoryAccess memory)
    {
        _addresses = addresses;
        _memory = memory;
        _reader = new GameMemoryReader(new MemoryReaderAdapter(memory));

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

    public void Tick()
    {
        UpdateNonZero(_runtime, Environment.TickCount64);

        var address = _addresses.HeroInfoAddress;
        if (address == 0)
            return;

        UpdateNonZero(_credits, _memory.ReadDouble(address + 0x178));
        UpdateNonZero(_uridium, _memory.ReadDouble(address + 0x180));
        UpdateNonZero(_experience, _memory.ReadDouble(address + 0x190));
        UpdateNonZero(_honor, _memory.ReadDouble(address + 0x198));

        _cargo.Track(_reader.ReadBindableInt(address + 0x148));
        _maxCargo.Track(_reader.ReadBindableInt(address + 0x150));
        _novaEnergy.Track(_reader.ReadBindableInt(address + 0x118));
        _teleportBonus.Track(ReadInt(address, 0x50));

        UserId = ReadInt(address, 0x30);
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

    private StatImpl CreateStat() => new(() => _updateStats);

    private void Register(IStatsApi.IStatKey key, StatImpl stat) =>
        _statistics[StatKey.From(key)] = stat;

    private double UpdateNonZero(StatImpl stat, double value)
    {
        if (value == 0)
            return 0;

        return stat.Track(value);
    }

    private int ReadInt(long address, int offset) => _reader.ReadInt(address + offset);

    private sealed class MemoryReaderAdapter(IGameMemoryAccess memory) : INativeMemory
    {
        public int ReadInt(long address) => memory.ReadInt(address);
        public long ReadLong(long address) => memory.ReadLong(address);
        public double ReadDouble(long address) => memory.ReadDouble(address);
    }
}
