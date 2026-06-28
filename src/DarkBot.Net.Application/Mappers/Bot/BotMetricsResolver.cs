using DarkBot.Net.Core.Interfaces.Bot;
using DarkBot.Net.Core.Game.Stats;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Mappers.Bot;

/// <summary>Общие расчёты метрик бота для UI snapshots.</summary>
internal static class BotMetricsResolver
{
    public static double ResolveMemoryMb(IStatsApi stats)
    {
        var tracked = stats.GetStatValue(Stats.Bot.Memory);
        return tracked > 0
            ? tracked
            : Environment.WorkingSet / (1024d * 1024d);
    }

    public static double ResolveLoopHz(IBotController bot)
    {
        var loopPeriodMs = bot.LastLoopPeriodMs;
        if (loopPeriodMs <= 0 && bot.LastTickMs > 0)
            loopPeriodMs = bot.LastTickMs;

        return loopPeriodMs > 0 ? 1000d / loopPeriodMs : 0d;
    }

    public static double ResolveLastTickMs(IBotController bot, IStatsApi stats)
    {
        var lastTickMs = bot.LastTickMs;
        if (lastTickMs > 0 || bot.TickCount == 0)
            return lastTickMs;

        var tracked = stats.GetStatValue(Stats.Bot.TickTime);
        return tracked > 0 ? tracked : lastTickMs;
    }
}
