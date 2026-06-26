using DarkBot.Net.Application.DTOs.Responses.Bot;

namespace DarkBot.Net.Application.Contracts;

/// <summary>Снимок состояния бота и карты для UI.</summary>
public interface IBotStatusAppService
{
    BotStatusSnapshot Capture();
}
