using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Game.Enums;
using EntityInfoStub = DarkBot.Net.Core.Entities.EntityInfoStub;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Portal из Frida snapshot (только данные).</summary>
public sealed class PortalEntity : IPortal
{
    public required int Id { get; init; }
    public required MutableLocationInfo Location { get; init; }
    public IReadOnlyCollection<int> Effects { get; init; } = [];

    public IGameMap? TargetMap => null;
    public int TypeId => 0;
    public IEntityInfo.Faction Faction => IEntityInfo.Faction.None;
    public bool IsJumping => false;

    public bool IsValid => Id > 0 && Location.IsInitialized;
    public bool IsSelectable => true;
    public IEntityInfo EntityInfo { get; } = new EntityInfoStub();
    ILocationInfo IEntity.LocationInfo => Location;
    public double X => Location.X;
    public double Y => Location.Y;

    public bool TrySelect(bool tryAttack) => false;
    public void SetMetadata(string key, object? value) { }
    public object? GetMetadata(string key) => null;
}
