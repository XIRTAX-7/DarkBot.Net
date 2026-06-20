namespace DarkBot.Net.Agent.Windows.Game;

public sealed class GameSessionStore
{
    private GameLaunchParameters? _current;

    public GameLaunchParameters? Current => _current;

    public bool HasSession => _current is not null;

    public void Save(GameLaunchParameters parameters) => _current = parameters;

    public void Clear() => _current = null;
}
