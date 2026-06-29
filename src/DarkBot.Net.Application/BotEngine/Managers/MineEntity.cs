using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Managers;
using EntityInfoStub = DarkBot.Net.Core.Entities.EntityInfoStub;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Mine из Frida snapshot (только данные, без collect/select в Ф1).</summary>
public sealed class MineEntity : IMine
{
    public required int Id { get; init; }
    public required MutableLocationInfo Location { get; init; }
    public IReadOnlyCollection<int> Effects { get; init; } = [];

    public bool IsValid => Id > 0 && Location.IsInitialized;
    public bool IsSelectable => false;
    public IEntityInfo EntityInfo { get; } = new EntityInfoStub();
    ILocationInfo IEntity.LocationInfo => Location;
    public double X => Location.X;
    public double Y => Location.Y;

    public bool TrySelect(bool tryAttack) => false;
    public void SetMetadata(string key, object? value) { }
    public object? GetMetadata(string key) => null;
}
