using DarkBot.Net.Core.Game.Enums;

namespace DarkBot.Net.Core.Game.Entities;

public interface IPortal : IEntity
{
    IGameMap? TargetMap { get; }
    int TypeId { get; }
    PortalType PortalType => PortalTypeExtensions.Of(TypeId);
    IEntityInfo.Faction Faction { get; }
    bool IsJumping { get; }
}
