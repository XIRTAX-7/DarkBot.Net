using DarkBot.Net.Core.Game.Items;

namespace DarkBot.Net.Core.Entities;

public class EmptySelectableItem : IItem
{
    public bool IsReady() => false;
}

public sealed class EmptyLaser : EmptySelectableItem, ISelectableItem.ILaser
{
    public static readonly EmptyLaser Instance = new();
}

public sealed class EmptyRocket : EmptySelectableItem, ISelectableItem.IRocket
{
    public static readonly EmptyRocket Instance = new();
}
