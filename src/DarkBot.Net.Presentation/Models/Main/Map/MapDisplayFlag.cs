namespace DarkBot.Net.Presentation.Models.Main.Map;

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

internal static class MapDisplayDefaults
{
    /// <summary>Значения по умолчанию как в Java DarkBot.</summary>
    public const MapDisplayFlag JavaDefaults =
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
}
