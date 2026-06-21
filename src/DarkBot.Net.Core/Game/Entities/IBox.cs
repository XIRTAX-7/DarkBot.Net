using DarkBot.Net.Core.Config.Types;

namespace DarkBot.Net.Core.Game.Entities;

public interface IBox : IEntity
{
    string Hash { get; }
    string TypeName { get; }
    IBoxInfo Info { get; }
    bool IsCollected { get; }
    bool TryCollect();
    void SetCollected();
    int Retries { get; }
    DateTimeOffset? CollectedUntil { get; }

    public interface IBeaconBox : IBox;
}
