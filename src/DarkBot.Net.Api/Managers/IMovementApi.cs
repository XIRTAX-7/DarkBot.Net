using DarkBot.Net.Api.Game;
using DarkBot.Net.Api.Game.Entities;

namespace DarkBot.Net.Api.Managers;

public interface IMovementApi : IApi.ISingleton
{
    ILocation CurrentLocation { get; }
    ILocation Destination { get; }
    IReadOnlyList<ILocatable> Path { get; }
    bool IsMoving { get; }
    bool WasMovingIn(long inTimeMs);
    bool IsOutOfMap { get; }
    bool CanMove(double x, double y);
    bool CanMove(ILocatable destination) => CanMove(destination.X, destination.Y);
    void MoveTo(double x, double y);
    void MoveTo(ILocatable destination) => MoveTo(destination.X, destination.Y);
    void MoveRandom();
    void Stop(bool currentLocation);
    void JumpPortal(IPortal portal);
    double GetClosestDistance(double x, double y);
    double GetClosestDistance(ILocatable destination) => GetClosestDistance(destination.X, destination.Y);
    double GetDistanceBetween(double x, double y, double ox, double oy);
    double GetDistanceBetween(ILocatable loc, ILocatable otherLoc) =>
        GetDistanceBetween(loc.X, loc.Y, otherLoc.X, otherLoc.Y);
    bool IsInPreferredZone(ILocatable location);
}
