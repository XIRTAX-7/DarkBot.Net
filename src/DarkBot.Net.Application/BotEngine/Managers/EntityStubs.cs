using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Application.BotEngine.State;

namespace DarkBot.Net.Application.BotEngine.Managers;

public sealed class ShipStub : IShip
{
    public required int Id { get; init; }
    public required EntityInfoStub EntityInfoData { get; init; }
    public required MutableLocationInfo Location { get; init; }
    public IReadOnlyCollection<int> Effects { get; init; } = [];

    public IEntity? Target => null;
    public bool IsAttacking => false;
    public ILockable.EntityLock Lock => ILockable.EntityLock.Unknown;
    public IHealth Health { get; } = new TrackedHealth();
    IEntityInfo ILockable.EntityInfo => EntityInfoData;
    ILocationInfo IEntity.LocationInfo => Location;
    public int ShipId => Id;
    public bool IsInvisible => false;
    public bool IsBlacklisted => false;
    public void SetBlacklisted(long timeMs) { }
    public bool IsValid => true;
    public bool IsSelectable => true;
    public bool TrySelect(bool tryAttack) => false;
    public bool IsMoving() => Location.IsMoving;
    public bool IsMoving(long inTimeMs) => Location.IsMoving;
    public int Speed => (int)Location.Speed;
    public double Angle => Location.Angle;
    public double DestinationAngle => Location.Angle;
    public bool IsAiming(ILocatable other) => false;
    public ILocation? Destination => null;
    public void SetMetadata(string key, object? value) { }
    public object? GetMetadata(string key) => null;
    public double X => Location.X;
    public double Y => Location.Y;
}

public sealed class StationStub : IStation.IRefinery
{
    public required int Id { get; init; }
    public required MutableLocationInfo Location { get; init; }

    public bool IsValid => Location.IsInitialized;
    public bool IsSelectable => false;
    public bool TrySelect(bool tryAttack) => false;
    public IEntityInfo EntityInfo { get; } = new EntityInfoStub();
    ILocationInfo IEntity.LocationInfo => Location;
    public IReadOnlyCollection<int> Effects { get; } = [];
    public void SetMetadata(string key, object? value) { }
    public object? GetMetadata(string key) => null;
    public double X => Location.X;
    public double Y => Location.Y;
}
