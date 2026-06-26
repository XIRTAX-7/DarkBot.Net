namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

/// <summary>Флаги отображения карты — порт Java Config.MapDisplay.TOGGLE.</summary>
[Flags]
public enum MapDisplayFlag
{
    None = 0,
    NpcNames = 1 << 0,
    Usernames = 1 << 1,
    ResourceNames = 1 << 2,
    ShowDestination = 1 << 3,
    ShowPet = 1 << 4,
    HeroName = 1 << 5,
    HpShieldNum = 1 << 6,
    Zones = 1 << 7,
    GroupArea = 1 << 8,
    GroupNames = 1 << 9,
    BoosterArea = 1 << 10,
    SortBoosters = 1 << 11,
    DevStuff = 1 << 12,
    MapStartStop = 1 << 13,
}

public sealed record MapRenderSettings(
    bool RoundEntities,
    int TrailLengthSec,
    double MapZoom,
    MapDisplayFlag DisplayFlags,
    bool CustomBackground,
    float CustomBackgroundOpacity);

internal static class MapRenderDefaults
{
    private const MapDisplayFlag JavaDisplayFlags =
        MapDisplayFlag.NpcNames
        | MapDisplayFlag.Usernames
        | MapDisplayFlag.ResourceNames
        | MapDisplayFlag.ShowDestination
        | MapDisplayFlag.ShowPet
        | MapDisplayFlag.HeroName
        | MapDisplayFlag.HpShieldNum
        | MapDisplayFlag.Zones
        | MapDisplayFlag.GroupArea
        | MapDisplayFlag.GroupNames
        | MapDisplayFlag.BoosterArea
        | MapDisplayFlag.SortBoosters;

    public static MapRenderSettings Java { get; } = new(
        RoundEntities: true,
        TrailLengthSec: 15,
        MapZoom: 1.0,
        DisplayFlags: JavaDisplayFlags,
        CustomBackground: false,
        CustomBackgroundOpacity: 0.3f);
}
