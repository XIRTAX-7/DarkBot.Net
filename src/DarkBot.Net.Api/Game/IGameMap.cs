namespace DarkBot.Net.Api.Game;

public interface IGameMap
{
    int Id { get; }
    string Name { get; }
    string? ShortName { get; }
    bool IsPvp { get; }
    bool IsGalaxyGate { get; }
}
