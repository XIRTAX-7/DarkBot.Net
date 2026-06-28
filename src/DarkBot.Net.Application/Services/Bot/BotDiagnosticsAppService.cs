using DarkBot.Net.Application.BotEngine.Loop;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Core.Game.Stats;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Services.Bot;

/// <summary>
/// Снимок метрик из bot loop и stats. Безопасен для частого вызова с UI-потока.
/// </summary>
public sealed class BotDiagnosticsAppService(IBotController bot, IStatsApi stats) : IBotDiagnosticsAppService
{
    public BotDiagnosticsSnapshot Get()
    {
        var lastTickMs = ResolveLastTickMs();
        var memoryMb = stats.GetStatValue(Stats.Bot.Memory);

        if (memoryMb <= 0)
            memoryMb = Environment.WorkingSet / (1024d * 1024d);

        var loopPeriodMs = bot.LastLoopPeriodMs;
        if (loopPeriodMs <= 0 && lastTickMs > 0)
            loopPeriodMs = lastTickMs;

        return new BotDiagnosticsSnapshot(
            LastTickMs: lastTickMs,
            MemoryMb: memoryMb,
            Ping: stats.Ping,
            LoopHz: loopPeriodMs > 0 ? 1000d / loopPeriodMs : 0d);
    }

    private double ResolveLastTickMs()
    {
        var lastTickMs = bot.LastTickMs;
        if (lastTickMs > 0 || bot.TickCount == 0)
            return lastTickMs;

        var tracked = stats.GetStatValue(Stats.Bot.TickTime);
        return tracked > 0 ? tracked : lastTickMs;
    }
}
