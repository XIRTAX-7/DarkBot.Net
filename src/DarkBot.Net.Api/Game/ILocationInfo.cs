namespace DarkBot.Net.Api.Game;

public interface ILocationInfo : ILocation
{
    bool IsMoving { get; }
    double Speed { get; }
    double Angle { get; }
    ILocation Current { get; }
    ILocation Last { get; }
    ILocation Past { get; }
    bool IsInitialized { get; }

    ILocation DestinationInTime(long timeMs)
    {
        var result = Copy();
        var speed = Speed;
        if (speed <= 0)
            return result;

        var move = speed * timeMs;
        var angle = Angle;
        return result.Plus(Math.Cos(angle) * move, Math.Sin(angle) * move);
    }
}
