using DarkBot.Net.Agent.Windows.Memory;

namespace DarkBot.Net.Agent.Windows.Bridge;

/// <summary>Backward-compatible wrapper over <see cref="NativeGameBridge"/> DarkMem surface.</summary>
public sealed class NativeMemoryBridge : INativeMemory, IDisposable
{
    private readonly NativeGameBridge _bridge = new();

    public void Initialize(string libDir, string classesDir) => _bridge.Initialize(libDir, classesDir);

    public int Version => _bridge.DarkMemVersion;

    public void OpenProcess(long pid) => _bridge.OpenProcess(pid);

    public int ReadInt(long address) => _bridge.ReadInt(address);

    public long ReadLong(long address) => _bridge.ReadLong(address);

    public double ReadDouble(long address) => _bridge.ReadDouble(address);

    public GameMemoryReader CreateReader() => _bridge.CreateDarkMemReader();

    public int ReadHeroHp(long shipAddress) => _bridge.ReadHeroHpFromDarkMem(shipAddress);

    public NativeGameBridge Inner => _bridge;

    public void Dispose() => _bridge.Dispose();
}
