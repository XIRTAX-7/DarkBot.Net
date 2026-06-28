using DarkBot.Net.Application.DTOs.Responses.Bot;

namespace DarkBot.Net.Application.Contracts;

/// <summary>
/// Read-only метрики бота для UI (title bar). Не вызывает Frida и не тикает менеджеры.
/// </summary>
public interface IBotDiagnosticsAppService
{
    BotDiagnosticsSnapshot Get();
}
