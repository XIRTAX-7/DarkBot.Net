using DarkBot.Net.Agent.Windows.Game;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Refresh / re-login hooks for credential sessions (Darkorbit-client reload).</summary>
public sealed class GameReloginService
{
    private readonly GameSessionStore _sessionStore;
    private readonly IGameConnection _game;
    private readonly GameLauncherService _launcher;
    private readonly ILogger<GameReloginService> _logger;
    private long _lastFailedLoginMs;

    public GameReloginService(
        GameSessionStore sessionStore,
        IGameConnection game,
        GameLauncherService launcher,
        ILogger<GameReloginService> logger)
    {
        _sessionStore = sessionStore;
        _game = game;
        _launcher = launcher;
        _logger = logger;
    }

    public bool CanRelogin =>
        _sessionStore.Current?.Username is not null && _sessionStore.Current.Password is not null;

    public void HandleRefresh(bool useFakeDailyLogin = true)
    {
        if (!CanRelogin)
        {
            _logger.LogInformation("Re-login unsupported for SID sessions — use Darkorbit-client reload");
            _game.HandleRefresh(useFakeDailyLogin);
            return;
        }

        if (_lastFailedLoginMs + 30_000 > Environment.TickCount64)
        {
            _logger.LogWarning("Last failed login was less than 30s ago — skipping re-login");
            return;
        }

        _logger.LogWarning("Credential re-login requires LoginService integration — reload client manually");
        _game.HandleRefresh(useFakeDailyLogin);
    }

    public void MarkLoginFailed() => _lastFailedLoginMs = Environment.TickCount64;
}
