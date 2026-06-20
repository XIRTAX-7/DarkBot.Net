namespace DarkBot.Net.Api.Game.Entities;

/// <summary>Port of eu.darkbot.api.game.entities.Station.</summary>
public interface IStation : IEntity
{
    interface IHeadquarter : IStation;
    interface IHangar : IStation;
    interface IRepair : IStation;
    interface ITurret : IStation;
    interface IRefinery : IStation;
    interface IQuestGiver : IStation;
    interface IHomeBase : IStation;
}
