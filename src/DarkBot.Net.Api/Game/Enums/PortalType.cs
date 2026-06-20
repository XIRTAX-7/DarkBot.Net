namespace DarkBot.Net.Api.Game.Enums;

public enum PortalType
{
    Unknown = -1,
    Standard = 1,
    Tutorial = 55,
    GroupGate = 34,
    Pirate = 51,
    PirateBroken = 52,
    Alpha = 2,
    Beta = 3,
    Gamma = 4,
    Delta = 5,
    Epsilon = 53,
    Zeta = 54,
    Kappa = 70,
    Lambda = 71,
    Kronos = 72,
    Hades = 74,
    Kuiper = 82,
    Quarantine = 84
}

public static class PortalTypeExtensions
{
    public static PortalType Of(int typeId)
    {
        foreach (PortalType type in Enum.GetValues<PortalType>())
        {
            if ((int)type == typeId)
                return type;
        }

        return PortalType.Unknown;
    }

    public static int GetId(this PortalType type) => (int)type;
}
