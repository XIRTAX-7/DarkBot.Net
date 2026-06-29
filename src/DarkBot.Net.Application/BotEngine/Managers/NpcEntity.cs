using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Interfaces.Game;
using EntityInfoStub = DarkBot.Net.Core.Entities.EntityInfoStub;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>NPC из Frida snapshot — выбор через bridge RPC selectEntity.</summary>
public sealed class NpcEntity(IUnityGameBridge bridge) : INpc
{
    private readonly EntityInfoStub _entityInfo = new();
    private readonly TrackedHealth _health = new();

    public required int Id { get; init; }
    public required MutableLocationInfo Location { get; init; }
    public string? Label { get; init; }
    public IReadOnlyCollection<int> Effects { get; init; } = [];

    public int NpcId => Id;
    public INpcInfo Info { get; } = new DefaultNpcInfo();
    public IEntity? Target => null;
    public bool IsAttacking => false;
    public ILockable.EntityLock Lock => ILockable.EntityLock.Unknown;
    public IHealth Health => _health;
    IEntityInfo ILockable.EntityInfo => _entityInfo;
    ILocationInfo IEntity.LocationInfo => Location;
    public int ShipId => Id;
    public bool IsInvisible => false;
    public bool IsBlacklisted => false;
    public bool IsValid => Id > 0 && Location.IsInitialized;
    public bool IsSelectable => true;
    public double X => Location.X;
    public double Y => Location.Y;
    public bool IsMoving() => Location.IsMoving;
    public bool IsMoving(long inTimeMs) => Location.IsMoving;
    public int Speed => (int)Location.Speed;
    public double Angle => Location.Angle;
    public double DestinationAngle => Location.Angle;
    public bool IsAiming(ILocatable other) => false;
    public ILocation? Destination => null;

    public bool TrySelect(bool tryAttack) =>
        bridge.SelectEntityAsync(Id, (int)X, (int)Y).GetAwaiter().GetResult();

    public void SetBlacklisted(long timeMs) { }
    public void SetMetadata(string key, object? value) { }
    public object? GetMetadata(string key) => null;

    private sealed class DefaultNpcInfo : INpcInfo
    {
        public bool ShouldKill { get; set; } = true;
        public string Name { get; init; } = string.Empty;
    }
}
