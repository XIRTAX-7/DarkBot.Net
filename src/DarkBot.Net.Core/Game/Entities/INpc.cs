using DarkBot.Net.Core.Config.Types;

namespace DarkBot.Net.Core.Game.Entities;

public interface INpc : IShip
{
    int NpcId { get; }
    INpcInfo Info { get; }
}
