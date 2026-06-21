using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Core.Entities;

public sealed class GameMapModel : IGameMap
{
    public GameMapModel(int id, string name, string? shortName = null, bool isPvp = false, bool isGalaxyGate = false)
    {
        Id = id;
        Name = name;
        ShortName = shortName ?? name;
        IsPvp = isPvp;
        IsGalaxyGate = isGalaxyGate;
    }

    public int Id { get; }
    public string Name { get; }
    public string? ShortName { get; }
    public bool IsPvp { get; }
    public bool IsGalaxyGate { get; }

    public override string ToString() => Name;
}
