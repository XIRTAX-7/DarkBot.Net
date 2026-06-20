namespace DarkBot.Net.Agent.Windows.Memory;

public static class GameMemoryConstants
{
    public const int BoolFalse = 0;
    public const int BoolTrue = 1;
    public const int BindableIntValueOffset = 0x38;
    public const long AtomKind = 0b111L;
    public const long AtomMask = ~AtomKind;
    public const int BadPtr = 0xFFFF;

    // Ship.health pointer offset (com.github.manolo8.darkbot.core.entities.Ship.update)
    public const int ShipHealthPointerOffset = 184;

    // Health.hp bindable field offset (com.github.manolo8.darkbot.core.objects.Health.update)
    public const int HealthHpOffset = 48;
}
