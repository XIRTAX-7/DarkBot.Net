using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Core.Entities;

public sealed class MutableLocationInfo : ILocationInfo
{
    public double X { get; private set; }
    public double Y { get; private set; }
    public bool IsMoving { get; private set; }
    public double Speed { get; private set; }
    public double Angle { get; private set; }
    public ILocation Current { get; private set; } = GameLocation.Of(0, 0);
    public ILocation Last { get; private set; } = GameLocation.Of(0, 0);
    public ILocation Past { get; private set; } = GameLocation.Of(0, 0);
    public bool IsInitialized { get; private set; }

    public ILocation SetTo(double x, double y)
    {
        Update(x, y, IsMoving, Speed, Angle);
        return Current;
    }

    public void Update(double x, double y, bool isMoving = false, double speed = 0, double angle = 0)
    {
        if (IsInitialized)
        {
            Past = Last;
            Last = Current;
        }

        X = x;
        Y = y;
        IsMoving = isMoving;
        Speed = speed;
        Angle = angle;
        Current = GameLocation.Of(x, y);
        IsInitialized = true;
    }
}
