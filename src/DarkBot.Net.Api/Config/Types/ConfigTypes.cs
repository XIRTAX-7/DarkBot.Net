namespace DarkBot.Net.Api.Config.Types;

public interface INpcInfo
{
    bool ShouldKill { get; set; }
    string Name { get; }
}

public interface IBoxInfo
{
    bool ShouldCollect { get; set; }
    int WaitTime { get; set; }
    int Priority { get; set; }
}

public interface IShipMode
{
    HeroConfiguration Configuration { get; }
    Game.Items.ISelectableItem.Formation Formation { get; }
}

public enum HeroConfiguration
{
    Unknown,
    First,
    Second
}

public static class HeroConfigurationExtensions
{
    public static HeroConfiguration Of(int config) => config switch
    {
        1 => HeroConfiguration.First,
        2 => HeroConfiguration.Second,
        _ => HeroConfiguration.Unknown
    };
}

public sealed record ShipMode(HeroConfiguration Configuration, Game.Items.ISelectableItem.Formation Formation)
{
    public static ShipMode Of(HeroConfiguration configuration, Game.Items.ISelectableItem.Formation formation) =>
        new(configuration, formation);
}

public sealed record PercentRange(double Min, double Max)
{
    public static PercentRange Of(double min, double max) => new(min, max);
}
