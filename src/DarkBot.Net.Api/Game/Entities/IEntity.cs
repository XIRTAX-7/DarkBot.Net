using DarkBot.Net.Api.Game.Enums;

namespace DarkBot.Net.Api.Game.Entities;

public interface IEntity : ILocatable
{
    double ILocatable.X => LocationInfo.X;
    double ILocatable.Y => LocationInfo.Y;

    int Id { get; }
    bool IsValid { get; }
    bool IsSelectable { get; }
    bool TrySelect(bool tryAttack);
    ILocationInfo LocationInfo { get; }

    IReadOnlyCollection<int> Effects { get; }

    bool HasEffect(int effect) => Effects.Contains(effect);

    bool HasEffect(EntityEffect entityEffect) => HasEffect(entityEffect.GetId());

    void SetMetadata(string key, object? value);
    object? GetMetadata(string key);
}
