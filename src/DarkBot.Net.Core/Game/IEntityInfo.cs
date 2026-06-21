namespace DarkBot.Net.Core.Game;

public interface IEntityInfo
{
    bool IsEnemy { get; }
    Faction EntityFaction { get; }
    string Username { get; }
    string ClanTag { get; }
    int ClanId { get; }
    Diplomacy ClanDiplomacy { get; }

    public enum Faction
    {
        None,
        Mmo,
        Eic,
        Vru,
        Saturn
    }

    public enum Diplomacy
    {
        None,
        Clan,
        Ally,
        War,
        NAP
    }
}
