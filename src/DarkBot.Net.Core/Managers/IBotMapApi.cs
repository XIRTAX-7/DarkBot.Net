namespace DarkBot.Net.Core.Managers;

/// <summary>Текущая карта бота — для модулей без привязки к MapManager.</summary>
public interface IBotMapApi : IApi.ISingleton
{
    int MapId { get; }
}
