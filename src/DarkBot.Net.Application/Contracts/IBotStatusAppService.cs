using DarkBot.Net.Application.Models.Bot;

namespace DarkBot.Net.Application.Contracts;

/// <summary>Снимок состояния бота и карты для UI.</summary>
public interface IBotStatusAppService
{
    BotStatusSnapshot Capture();
}
