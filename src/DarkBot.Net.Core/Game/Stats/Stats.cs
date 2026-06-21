using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Core.Game.Stats;

public static class Stats
{
    public static class General
    {
        public static readonly IStatsApi.IStatKey Credits = Key(nameof(Credits));
        public static readonly IStatsApi.IStatKey Uridium = Key(nameof(Uridium));
        public static readonly IStatsApi.IStatKey Experience = Key(nameof(Experience));
        public static readonly IStatsApi.IStatKey Honor = Key(nameof(Honor));
        public static readonly IStatsApi.IStatKey Cargo = Key(nameof(Cargo));
        public static readonly IStatsApi.IStatKey MaxCargo = Key(nameof(MaxCargo));
        public static readonly IStatsApi.IStatKey NovaEnergy = Key(nameof(NovaEnergy));
        public static readonly IStatsApi.IStatKey TeleportBonusAmount = Key(nameof(TeleportBonusAmount));
    }

    public static class Bot
    {
        public static readonly IStatsApi.IStatKey Ping = BotKey(nameof(Ping));
        public static readonly IStatsApi.IStatKey TickTime = BotKey(nameof(TickTime));
        public static readonly IStatsApi.IStatKey Memory = BotKey(nameof(Memory));
        public static readonly IStatsApi.IStatKey Cpu = BotKey(nameof(Cpu));
        public static readonly IStatsApi.IStatKey Runtime = BotKey(nameof(Runtime));

        private static IStatsApi.IStatKey BotKey(string name) => new StatKey(null, nameof(Bot), name);
    }

    private static IStatsApi.IStatKey Key(string name) => new StatKey(null, nameof(General), name);

    private sealed record StatKey(string? Namespace, string Category, string Name)
        : IStatsApi.IStatKey;
}
