using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Core.Entities;

/// <summary>Placeholder для entity info до typed entities (Phase 4).</summary>
public sealed class EntityInfoStub : IEntityInfo
{
    public bool IsEnemy => false;
    public IEntityInfo.Faction EntityFaction => IEntityInfo.Faction.None;
    public string Username { get; init; } = string.Empty;
    public string ClanTag { get; init; } = string.Empty;
    public int ClanId { get; init; }
    public IEntityInfo.Diplomacy ClanDiplomacy => IEntityInfo.Diplomacy.None;
}
