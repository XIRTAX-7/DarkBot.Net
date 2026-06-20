using DarkBot.Net.Api.Config.Types;

namespace DarkBot.Net.Api.Game.Entities;

public interface INpc : IShip
{
    int NpcId { get; }
    INpcInfo Info { get; }
}
