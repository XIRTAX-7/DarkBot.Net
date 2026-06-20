namespace DarkBot.Net.Api.Game.Entities;

public interface ILockable : IEntity
{
    EntityLock Lock { get; }
    IHealth Health { get; }
    IEntityInfo EntityInfo { get; }

    bool IsOwned => Lock == EntityLock.Owned;

    public enum EntityLock
    {
        Unknown,
        Owned,
        NotOwned,
        Purple,
        GrayDark
    }
}

public static class EntityLockExtensions
{
    public static ILockable.EntityLock Of(int lockId)
    {
        var values = Enum.GetValues<ILockable.EntityLock>();
        return lockId >= 0 && lockId < values.Length ? values[lockId] : ILockable.EntityLock.Unknown;
    }
}
