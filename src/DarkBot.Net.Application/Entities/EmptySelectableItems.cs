using DarkBot.Net.Core.Game.Items;

namespace DarkBot.Net.Application.Entities;

internal class EmptySelectableItem : IItem
{
    public bool IsReady() => false;
}

internal sealed class EmptyLaser : EmptySelectableItem, ISelectableItem.ILaser
{
    public static readonly EmptyLaser Instance = new();
}

internal sealed class EmptyRocket : EmptySelectableItem, ISelectableItem.IRocket
{
    public static readonly EmptyRocket Instance = new();
}
