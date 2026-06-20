namespace DarkBot.Net.Api.Game.Entities;

public interface IMovable : IEntity
{
    bool IsMoving();
    bool IsMoving(long inTimeMs);
    int Speed { get; }
    double Angle { get; }
    double DestinationAngle { get; }
    bool IsAiming(ILocatable other);
    ILocation? Destination { get; }

    long TimeTo(double distance) => Speed == 0 ? long.MaxValue : (long)(distance * 1000 / Speed);

    long TimeTo(ILocatable destination) => TimeTo(LocationInfo.DistanceTo(destination));
}
