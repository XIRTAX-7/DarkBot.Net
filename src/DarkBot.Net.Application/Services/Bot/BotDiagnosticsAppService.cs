using DarkBot.Net.Core.Interfaces.Bot;

using DarkBot.Net.Application.Contracts;

using DarkBot.Net.Application.DTOs.Responses.Bot;

using DarkBot.Net.Application.Mappers.Bot;

using DarkBot.Net.Core.Managers;



namespace DarkBot.Net.Application.Services.Bot;



/// <summary>

/// Снимок метрик из bot loop и stats. Безопасен для частого вызова с UI-потока.

/// </summary>

public sealed class BotDiagnosticsAppService(IBotController bot, IStatsApi stats) : IBotDiagnosticsAppService

{

    public BotDiagnosticsSnapshot Get() =>

        new(

            LastTickMs: BotMetricsResolver.ResolveLastTickMs(bot, stats),

            MemoryMb: BotMetricsResolver.ResolveMemoryMb(stats),

            Ping: stats.Ping,

            LoopHz: BotMetricsResolver.ResolveLoopHz(bot));

}

