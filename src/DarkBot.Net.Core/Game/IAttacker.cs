using DarkBot.Net.Core.Game.Entities;

namespace DarkBot.Net.Core.Game;

public interface IAttacker : ILockable
{
    IEntity? Target { get; }
    bool IsAttacking { get; }

    T? GetTargetAs<T>() where T : class, IEntity =>
        Target as T;

    bool IsAttackingTarget(ILockable other) =>
        IsAttacking && Equals(Target, other);
}
