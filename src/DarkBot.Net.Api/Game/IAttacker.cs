using DarkBot.Net.Api.Game.Entities;

namespace DarkBot.Net.Api.Game;

public interface IAttacker : ILockable
{
    IEntity? Target { get; }
    bool IsAttacking { get; }

    T? GetTargetAs<T>() where T : class, IEntity =>
        Target as T;

    bool IsAttackingTarget(ILockable other) =>
        IsAttacking && Equals(Target, other);
}
