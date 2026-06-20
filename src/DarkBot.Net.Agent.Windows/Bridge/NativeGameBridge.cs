using System.Text;
using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Agent.Windows.Memory;
using DarkBot.Net.Agent.Windows.Native;

namespace DarkBot.Net.Agent.Windows.Bridge;

/// <summary>Unified native bridge: DarkMem (attach) + optional KekkaPlayer (login).</summary>
public sealed class NativeGameBridge : INativeMemory, IDisposable
{
    private bool _initialized;

    public NativeGameBridge() => Kekka = new NativeKekkaBridge(this);

    public NativeKekkaBridge Kekka { get; }

    public bool IsInitialized => _initialized;

    public string LastNativeError => DarkBotBridgeNative.GetLastError();

    public void Initialize(string libDir, string classesDir, string? workingDir = null)
    {
        if (_initialized)
            return;

        var code = DarkBotBridgeNative.bridge_init(libDir, classesDir, workingDir);
        if (code != 0)
            throw new NativeBridgeException($"bridge_init failed ({code}): {DarkBotBridgeNative.GetLastError()}");

        _initialized = true;
    }

    public int DarkMemVersion
    {
        get
        {
            EnsureInitialized();
            return DarkBotBridgeNative.bridge_get_version();
        }
    }

    public bool IsKekkaAvailable
    {
        get
        {
            if (!_initialized)
                return false;

            return DarkBotBridgeNative.bridge_kekka_is_available() == 1;
        }
    }

    public void OpenProcess(long pid)
    {
        EnsureInitialized();
        DarkBotBridgeNative.bridge_open_process(pid);
    }

    public int ReadInt(long address)
    {
        EnsureInitialized();
        return DarkBotBridgeNative.bridge_read_int(address);
    }

    public long ReadLong(long address)
    {
        EnsureInitialized();
        return DarkBotBridgeNative.bridge_read_long(address);
    }

    public double ReadDouble(long address)
    {
        EnsureInitialized();
        return DarkBotBridgeNative.bridge_read_double(address);
    }

    public GameMemoryReader CreateDarkMemReader() => new(this);

    public int ReadHeroHpFromDarkMem(long shipAddress) => CreateDarkMemReader().ReadHeroHp(shipAddress);

    public int ReadHeroHp(long shipAddress) =>
        IsKekkaAvailable ? Kekka.ReadHeroHp(shipAddress) : ReadHeroHpFromDarkMem(shipAddress);

    public IReadOnlyList<GameProcessInfo> GetProcesses()
    {
        EnsureInitialized();
        var count = DarkBotBridgeNative.bridge_darkmem_get_process_count();
        if (count <= 0)
            return Array.Empty<GameProcessInfo>();

        var processes = new List<GameProcessInfo>(count);
        var nameBuffer = new byte[256];
        for (var i = 0; i < count; i++)
        {
            var pid = DarkBotBridgeNative.bridge_darkmem_get_process_pid(i);
            var nameLength = DarkBotBridgeNative.bridge_darkmem_get_process_name(i, nameBuffer, nameBuffer.Length);
            var name = nameLength > 0
                ? Encoding.UTF8.GetString(nameBuffer, 0, nameLength)
                : $"pid-{pid}";
            processes.Add(new GameProcessInfo { Pid = pid, Name = name });
        }

        return processes;
    }

    public void Dispose()
    {
        if (!_initialized)
            return;

        DarkBotBridgeNative.bridge_shutdown();
        _initialized = false;
    }

    internal void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("Native bridge is not initialized.");
    }
}
