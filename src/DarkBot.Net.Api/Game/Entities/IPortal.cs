using DarkBot.Net.Api.Game.Enums;

namespace DarkBot.Net.Api.Game.Entities;

public interface IPortal : IEntity
{
    IGameMap? TargetMap { get; }
    int TypeId { get; }
    PortalType PortalType => PortalTypeExtensions.Of(TypeId);
    IEntityInfo.Faction Faction { get; }
    bool IsJumping { get; }
}
