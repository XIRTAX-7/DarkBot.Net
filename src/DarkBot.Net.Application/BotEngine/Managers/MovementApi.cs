using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Application.BotEngine.Addresses;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Movement via Unity Frida bridge.</summary>
public sealed class MovementApi : IMovementApi
{
    private readonly BotAddressRegistry _addresses;
    private readonly IUnityGameBridge _unityBridge;
    private readonly MapManager _map;
    private readonly HeroManager _hero;
    private readonly ILogger<MovementApi> _logger;
    private readonly MutableLocationInfo _location = new();
    private ILocation _destination = GameLocation.Of(0, 0);
    private readonly List<ILocatable> _path = [];

    public MovementApi(
        BotAddressRegistry addresses,
        IUnityGameBridge unityBridge,
        MapManager map,
        HeroManager hero,
        ILogger<MovementApi> logger)
    {
        _addresses = addresses;
        _unityBridge = unityBridge;
        _map = map;
        _hero = hero;
        _logger = logger;
    }

    public ILocation CurrentLocation => _location;
    public ILocation Destination => _destination;
    public IReadOnlyList<ILocatable> Path => _path;
    public bool IsMoving => _location.IsMoving;
    public bool IsOutOfMap =>
        MapCoordinateBounds.IsOutOfBounds(_hero.X, _hero.Y, _map.InternalWidth, _map.InternalHeight);

    public bool WasMovingIn(long inTimeMs) => _location.IsMoving;

    public bool CanMove(double x, double y) =>
        _addresses.HasScreenManager
        && x >= 0
        && y >= 0
        && MapCoordinateBounds.IsInSafeBounds(x, y, _map.InternalWidth, _map.InternalHeight);

    public void MoveTo(double x, double y)
    {
        if (!TryPrepareMove(x, y, out var clampedX, out var clampedY))
            return;

        // Sync path — только bot loop (10 Hz background). UI использует MoveToAsync.
        _unityBridge.MoveToAsync((int)clampedX, (int)clampedY).GetAwaiter().GetResult();
    }

    public async Task MoveToAsync(double x, double y, CancellationToken cancellationToken = default)
    {
        if (!TryPrepareMove(x, y, out var clampedX, out var clampedY))
            return;

        await _unityBridge.MoveToAsync((int)clampedX, (int)clampedY, cancellationToken).ConfigureAwait(false);
    }

    public void MoveRandom()
    {
        var random = Random.Shared;
        var margin = MapCoordinateBounds.SafeMargin;
        var width = Math.Max(_map.InternalWidth - margin, margin + 1);
        var height = Math.Max(_map.InternalHeight - margin, margin + 1);
        MoveTo(random.Next(margin, width), random.Next(margin, height));
    }

    public void Stop(bool currentLocation)
    {
        if (currentLocation)
            _destination = GameLocation.Of(_location.X, _location.Y);

        _location.Update(_location.X, _location.Y, isMoving: false);
    }

    public void JumpPortal(IPortal portal)
    {
        if (portal is null || !_addresses.HasScreenManager)
            return;

        MoveTo(portal.X, portal.Y);
    }

    public double GetClosestDistance(double x, double y) =>
        Math.Sqrt(Math.Pow(_location.X - x, 2) + Math.Pow(_location.Y - y, 2));

    public double GetDistanceBetween(double x, double y, double ox, double oy) =>
        Math.Sqrt(Math.Pow(x - ox, 2) + Math.Pow(y - oy, 2));

    public bool IsInPreferredZone(ILocatable location) => true;

    private bool TryPrepareMove(double x, double y, out double clampedX, out double clampedY)
    {
        clampedX = 0;
        clampedY = 0;

        if (x < 0 || y < 0)
        {
            _logger.LogWarning(
                "MoveTo rejected — negative coordinates ({X:F1},{Y:F1}) would fly into radiation",
                x, y);
            return false;
        }

        (clampedX, clampedY) = MapCoordinateBounds.SafeClamp(x, y, _map.InternalWidth, _map.InternalHeight);
        _destination = GameLocation.Of(clampedX, clampedY);
        _location.Update(_hero.X, _hero.Y, isMoving: true);

        _logger.LogInformation(
            "MoveTo requested=({RequestX:F1},{RequestY:F1}) safe=({ClampedX:F1},{ClampedY:F1}) " +
            "map={MapW}x{MapH} hero=({HeroX:F0},{HeroY:F0}) bridgeReady={BridgeReady}",
            x, y, clampedX, clampedY,
            _map.InternalWidth, _map.InternalHeight,
            _hero.X, _hero.Y,
            _addresses.HasScreenManager);

        if (!_addresses.HasScreenManager)
        {
            _logger.LogWarning("MoveTo skipped — bridge not ready");
            return false;
        }

        if (MapCoordinateBounds.IsOutOfBounds(_hero.X, _hero.Y, _map.InternalWidth, _map.InternalHeight))
        {
            _logger.LogWarning(
                "MoveTo while hero is out of map at ({HeroX:F0},{HeroY:F0}) — command still sent to return",
                _hero.X, _hero.Y);
        }

        return true;
    }
}
