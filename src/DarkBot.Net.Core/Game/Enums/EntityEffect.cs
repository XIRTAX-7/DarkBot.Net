namespace DarkBot.Net.Core.Game.Enums;

public enum EntityEffect
{
    Undefined = -1,
    Locator = 1,
    PetSpawn = 2,
    EnergyLeech = 11,
    NpcIsh = 16,
    BoxCollecting = 20,
    BootyCollecting = 21,
    DrawFire = 36,
    StickyBomb = 56,
    PolarityPositive = 65,
    PolarityNegative = 66,
    RepairBot = 76,
    Ish = 84,
    Infection = 85
}

public static class EntityEffectExtensions
{
    public static int GetId(this EntityEffect effect) => (int)effect;
}
