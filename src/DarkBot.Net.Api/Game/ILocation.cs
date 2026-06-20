namespace DarkBot.Net.Api.Game;

public interface ILocation : ILocatable
{
    ILocation SetTo(double x, double y);

    ILocation SetTo(ILocatable other) => SetTo(other.X, other.Y);

    ILocation Copy() => GameLocation.Of(this);

    ILocation Plus(double plusX, double plusY) => SetTo(X + plusX, Y + plusY);

    ILocation Plus(ILocatable other) => Plus(other.X, other.Y);

    ILocation ToAngle(ILocatable center, double angle, double radius) =>
        SetTo(center.X - Math.Cos(angle) * radius, center.Y - Math.Sin(angle) * radius);

    ILocation ToAngle(double angle, double radius) => ToAngle(this, angle, radius);
}

public sealed record GameLocation(double X, double Y) : ILocation
{
    public static GameLocation Of(double x, double y) => new(x, y);

    public static GameLocation Of(ILocatable locatable) => Of(locatable.X, locatable.Y);

    public static GameLocation Of(ILocatable center, double angle, double radius) =>
        Of(center.X - Math.Cos(angle) * radius, center.Y - Math.Sin(angle) * radius);

    public ILocation SetTo(double x, double y) => this with { X = x, Y = y };
}
