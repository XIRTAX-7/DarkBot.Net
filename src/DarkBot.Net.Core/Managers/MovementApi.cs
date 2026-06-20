using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Api.Game;
using DarkBot.Net.Api.Game.Entities;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Managers;

/// <summary>Movement via Frida game API (Darkorbit-client).</summary>
public sealed class MovementApi : IMovementApi
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameConnection _game;
    private readonly MutableLocationInfo _location = new();
    private ILocation _destination = GameLocation.Of(0, 0);
    private readonly List<ILocatable> _path = [];

    public MovementApi(BotAddressRegistry addresses, IGameConnection game)
    {
        _addresses = addresses;
        _game = game;
    }

    public ILocation CurrentLocation => _location;
    public ILocation Destination => _destination;
    public IReadOnlyList<ILocatable> Path => _path;
    public bool IsMoving => _location.IsMoving;
    public bool IsOutOfMap => false;

    public bool WasMovingIn(long inTimeMs) => _location.IsMoving;

    public bool CanMove(double x, double y) => _addresses.HasScreenManager;

    public void MoveTo(double x, double y)
    {
        _destination = GameLocation.Of(x, y);
        _location.Update(_location.X, _location.Y, isMoving: true);

        if (_addresses.HasScreenManager)
            _game.MoveShip(_addresses.ScreenManagerAddress, (long)x, (long)y);
    }

    public void MoveRandom()
    {
        var random = Random.Shared;
        MoveTo(random.Next(1000, 20000), random.Next(1000, 13000));
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
}
