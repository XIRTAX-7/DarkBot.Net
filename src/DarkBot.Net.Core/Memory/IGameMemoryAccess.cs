namespace DarkBot.Net.Core.Memory;

/// <summary>Abstraction over native memory reads for managers and tests.</summary>
public interface IGameMemoryAccess
{
    int ReadInt(long address);
    long ReadLong(long address);
    double ReadDouble(long address);
    int ReadHeroHp(long shipAddress);
}
