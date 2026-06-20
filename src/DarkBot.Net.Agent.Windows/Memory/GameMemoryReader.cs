namespace DarkBot.Net.Agent.Windows.Memory;

/// <summary>Port of com.github.manolo8.darkbot.core.MemoryAPI pointer helpers.</summary>
public sealed class GameMemoryReader(INativeMemory memory)
{
    public int ReadInt(long address)
    {
        if (!GameMemoryUtil.IsValidPtr(address))
            return 0;
        return memory.ReadInt(address);
    }

    public int ReadInt(long address, int o1) => ReadInt(address + o1);

    public int ReadInt(long address, int o1, int o2) =>
        ReadInt(ReadAtom(address, o1) + o2);

    public long ReadLong(long address)
    {
        if (!GameMemoryUtil.IsValidPtr(address))
            return 0;
        return memory.ReadLong(address);
    }

    public long ReadLong(long address, int o1) => ReadLong(address + o1);

    public long ReadLong(long address, int o1, int o2) =>
        ReadLong(ReadLong(address, o1) + o2);

    public double ReadDouble(long address)
    {
        if (!GameMemoryUtil.IsValidPtr(address))
            return 0;
        return memory.ReadDouble(address);
    }

    public double ReadDouble(long address, int o1) => ReadDouble(address + o1);

    public double ReadDouble(long address, int o1, int o2) =>
        ReadDouble(ReadAtom(address, o1) + o2);

    public long ReadAtom(long address) => ReadLong(address) & GameMemoryConstants.AtomMask;

    public long ReadAtom(long address, int o1) => ReadAtom(address + o1);

    public int ReadBindableInt(long address) =>
        ClampDoubleToInt(ReadDouble(address, GameMemoryConstants.BindableIntValueOffset));

    public int ReadBindableInt(long address, int o1) =>
        ClampDoubleToInt(ReadDouble(address, o1, GameMemoryConstants.BindableIntValueOffset));

    public int ReadHeroHp(long shipAddress)
    {
        if (!GameMemoryUtil.IsValidPtr(shipAddress))
            return 0;

        var healthAddress = ReadLong(shipAddress + GameMemoryConstants.ShipHealthPointerOffset);
        return ReadBindableInt(healthAddress, GameMemoryConstants.HealthHpOffset);
    }

    public static int ClampDoubleToInt(double value)
    {
        if (value > int.MaxValue)
            return int.MaxValue;
        if (value < int.MinValue)
            return int.MinValue;
        return (int)value;
    }
}
