namespace DarkBot.Net.Api.Game;

public interface ILocatable
{
    double X { get; }
    double Y { get; }

    int IntX => (int)X;
    int IntY => (int)Y;

    bool IsSameAs(ILocatable other) =>
        other is not null &&
        X.Equals(other.X) &&
        Y.Equals(other.Y);

    double DistanceTo(double ox, double oy)
    {
        var dx = ox - X;
        var dy = oy - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    double DistanceTo(ILocatable other) => DistanceTo(other.X, other.Y);

    double AngleTo(double ox, double oy) => Math.Atan2(Y - oy, X - ox);

    double AngleTo(ILocatable other) => AngleTo(other.X, other.Y);
}

public readonly record struct LocatablePoint(double X, double Y) : ILocatable
{
    public static LocatablePoint Of(double x, double y) => new(x, y);

    public static LocatablePoint Of(ILocatable center, double angle, double radius) =>
        Of(center.X - Math.Cos(angle) * radius, center.Y - Math.Sin(angle) * radius);
}
