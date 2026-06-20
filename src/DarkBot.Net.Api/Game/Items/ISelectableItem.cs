namespace DarkBot.Net.Api.Game.Items;

public interface ISelectableItem
{
    public interface ILaser : ISelectableItem;
    public interface IRocket : ISelectableItem;

    public enum Formation
    {
        Standard,
        Turtle,
        Arrow,
        Lance,
        Star,
        Pincer,
        DoubleArrow,
        Diamond,
        Chevron,
        Moth,
        Crab
    }
}

public interface IItem
{
    bool IsReady();
}
