namespace DarkBot.Net.Agent.Windows.Memory;

public interface INativeMemory
{
    int ReadInt(long address);
    long ReadLong(long address);
    double ReadDouble(long address);
}
