namespace DarkBot.Net.Api.Game.Entities;

public interface IShip : IAttacker, IMovable
{
    int ShipId { get; }
    bool IsInvisible { get; }
    bool IsBlacklisted { get; }
    void SetBlacklisted(long timeMs);
}
