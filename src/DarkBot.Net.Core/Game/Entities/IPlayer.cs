using DarkBot.Net.Core.Game.Items;

namespace DarkBot.Net.Core.Game.Entities;

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
