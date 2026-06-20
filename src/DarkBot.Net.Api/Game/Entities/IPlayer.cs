using DarkBot.Net.Api.Game.Items;

namespace DarkBot.Net.Api.Game.Entities;

public interface IPlayer : IShip
{
    string ShipType { get; }
    bool HasPet { get; }
    IPet? Pet { get; }
    ISelectableItem.Formation Formation { get; }
    bool IsInFormation(int formationId);
    bool IsInFormation(ISelectableItem.Formation formation) => IsInFormation((int)formation);
}

public interface IPet : IShip;
