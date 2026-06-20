using DarkBot.Net.Agent.Windows.Game;

namespace DarkBot.Net.Agent.Windows.Memory;

/// <summary>Port of ByteUtils.ExtraMemoryReader.searchClassClosure.</summary>
public sealed class ExtraMemoryReader
{
    private const int MaxTableSize = 2 << 20;

    private readonly IGameConnection _game;
    private readonly Func<long> _mainApplicationAddressProvider;
    private byte[]? _tableData;
    private long _lastTableRefreshMs;

    public ExtraMemoryReader(IGameConnection game, Func<long> mainApplicationAddressProvider)
    {
        _game = game;
        _mainApplicationAddressProvider = mainApplicationAddressProvider;
    }

    public long SearchClassClosure(Func<long, bool> pattern)
    {
        var mainApplicationAddress = _mainApplicationAddressProvider();
        if (mainApplicationAddress == 0)
            return 0;

        var table = _game.ReadLong(mainApplicationAddress + 0x10);
        table = ReadPointerChain(table, 0x10, 0x10, 0x18);
        table = ReadPointerChain(table, 0x10, 0x28);
        var capacity = _game.ReadInt(table + 8) * 8;

        if (capacity <= 0 || capacity > MaxTableSize)
            return 0;

        RefreshTable(table, capacity);

        for (var i = 0; i < capacity; i += 8)
        {
            var entry = GetLong(_tableData!, i);
            if (entry == 0)
                continue;

            var closure = _game.ReadLong(entry + 0x20);
            if (closure == 0 || closure == 0x200000001L)
                continue;

            if (pattern(closure))
                return closure;
        }

        return 0;
    }

    public void ResetCache()
    {
        _tableData = null;
        _lastTableRefreshMs = 0;
    }

    private void RefreshTable(long table, int capacity)
    {
        var now = Environment.TickCount64;
        if (_tableData is null || _tableData.Length < capacity)
        {
            _tableData = new byte[capacity];
            ReadTableBytes(table, capacity);
            _lastTableRefreshMs = now;
            return;
        }

        if (now - _lastTableRefreshMs >= 750)
        {
            ReadTableBytes(table, capacity);
            _lastTableRefreshMs = now;
        }
    }

    private void ReadTableBytes(long table, int capacity)
    {
        for (var offset = 0; offset + 8 <= capacity; offset += 8)
        {
            var value = _game.ReadLong(table + 0x10 + offset);
            WriteLong(_tableData!, offset, value);
        }
    }

    private long ReadPointerChain(long baseAddress, params int[] offsets)
    {
        var address = baseAddress;
        foreach (var offset in offsets)
            address = _game.ReadLong(address + offset);

        return address;
    }

    private static long GetLong(byte[] data, int offset) =>
        ((long)data[offset + 7] << 56) |
        (((long)data[offset + 6] & 0xff) << 48) |
        (((long)data[offset + 5] & 0xff) << 40) |
        (((long)data[offset + 4] & 0xff) << 32) |
        (((long)data[offset + 3] & 0xff) << 24) |
        (((long)data[offset + 2] & 0xff) << 16) |
        (((long)data[offset + 1] & 0xff) << 8) |
        ((long)data[offset] & 0xff);

    private static void WriteLong(byte[] data, int offset, long value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
        data[offset + 4] = (byte)(value >> 32);
        data[offset + 5] = (byte)(value >> 40);
        data[offset + 6] = (byte)(value >> 48);
        data[offset + 7] = (byte)(value >> 56);
    }
}
