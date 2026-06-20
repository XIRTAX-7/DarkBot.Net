using DarkBot.Net.Api.Events;
using DarkBot.Net.Api.Game.Stats;

namespace DarkBot.Net.Api.Managers;

public interface IStatsApi : IApi.ISingleton
{
    IStat GetStat(IStatKey key);
    double GetStatValue(IStatKey key) => GetStat(key).Current;
    IStat RegisterStat(IStatKey key);
    void SetStatValue(IStatKey key, double newValue);
    void ResetStats();

    int Ping => (int)GetStatValue(Stats.Bot.Ping);
    int Level => Math.Max(1, (int)(Math.Log(TotalExperience / 10_000) / Math.Log(2)) + 2);
    int Cargo => (int)GetStatValue(Stats.General.Cargo);
    int MaxCargo => (int)GetStatValue(Stats.General.MaxCargo);
    TimeSpan RunningTime => TimeSpan.FromMilliseconds((long)GetStat(Stats.Bot.Runtime).Earned);
    double TotalCredits => GetStatValue(Stats.General.Credits);
    double EarnedCredits => GetStat(Stats.General.Credits).Earned;
    double TotalUridium => GetStatValue(Stats.General.Uridium);
    double EarnedUridium => GetStat(Stats.General.Uridium).Earned;
    double TotalExperience => GetStatValue(Stats.General.Experience);
    double EarnedExperience => GetStat(Stats.General.Experience).Earned;
    double TotalHonor => GetStatValue(Stats.General.Honor);
    double EarnedHonor => GetStat(Stats.General.Honor).Earned;
    int NovaEnergy => (int)GetStatValue(Stats.General.NovaEnergy);

    interface IStatKey
    {
        string? Namespace { get; }
        string Category { get; }
        string Name { get; }
    }

    interface IStat
    {
        double Initial { get; }
        double Earned { get; }
        double Spent { get; }
        double Current { get; }
        ITimeSeries? TimeSeries { get; }
    }

    interface ITimeSeries
    {
        IReadOnlyList<long> Time { get; }
        IReadOnlyList<double> Value { get; }
    }

    sealed class StatsResetEvent : IEvent;
}
