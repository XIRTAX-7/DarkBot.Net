using DarkBot.Net.Application.DTOs.Responses.Bot;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Presentation.Resources;

namespace DarkBot.Net.Presentation.Formatting;

/// <summary>Форматирование status line главного экрана из snapshot + connection status.</summary>
internal static class MainStatusLineFormatter
{
    public static string Format(MapStatusSnapshot map, GameConnectionStatusSnapshot connectionStatus)
    {
        var gameStatusLine = GameConnectionStatusFormatter.Format(connectionStatus);

        if (map.Hero.Valid)
        {
            return UiStrings.Format(
                nameof(UiStrings.Status_HpFormat),
                map.MapName,
                map.Hero.Hp,
                map.Hero.MaxHp);
        }

        if (map.Hero.OnMap)
        {
            return UiStrings.Format(
                nameof(UiStrings.Status_PositionFormat),
                map.MapName,
                map.Hero.X,
                map.Hero.Y,
                gameStatusLine);
        }

        if (map.MapId == -1)
            return gameStatusLine;

        return UiStrings.Format(
            nameof(UiStrings.Status_MapFormat),
            map.MapName,
            gameStatusLine);
    }
}
