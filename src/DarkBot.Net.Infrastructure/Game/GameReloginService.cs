using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Refresh / re-login hooks for credential sessions (Darkorbit-client reload).</summary>
public sealed class GameReloginService
{
    private readonly GameSessionStore _sessionStore;
    private readonly IGameConnection _game;
    private readonly GameLauncherService _launcher;
    private readonly GamePacketReader _packetReader;
    private readonly ILogger<GameReloginService> _logger;
    private long _lastFailedLoginMs;
    private long _lastInvalidSessionMs;

    public GameReloginService(
        GameSessionStore sessionStore,
        IGameConnection game,
        GameLauncherService launcher,
        GamePacketReader packetReader,
        ILogger<GameReloginService> logger)
    {
        _sessionStore = sessionStore;
        _game = game;
        _launcher = launcher;
        _packetReader = packetReader;
        _logger = logger;

        _packetReader.InvalidSessionDetected += OnInvalidSessionPacket;
    }

    private void OnInvalidSessionPacket(GamePacketMessage message)
    {
        if (_lastInvalidSessionMs + 60_000 > Environment.TickCount64)
            return;

        _lastInvalidSessionMs = Environment.TickCount64;
        _logger.LogWarning("Invalid session packet detected ({Name}) — requesting client refresh", message.Name);
        HandleRefresh();
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
