using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Tests.Fakes;

public sealed class FakeGameMemoryAccess : IGameMemoryAccess
{
    private readonly Dictionary<long, int> _ints = new();
    private readonly Dictionary<long, long> _longs = new();
    private readonly Dictionary<long, double> _doubles = new();
    private readonly Dictionary<long, int> _heroHp = new();

    public void SetInt(long address, int value) => _ints[address] = value;
    public void SetLong(long address, long value) => _longs[address] = value;
    public void SetDouble(long address, double value) => _doubles[address] = value;
    public void SetHeroHp(long shipAddress, int hp) => _heroHp[shipAddress] = hp;

    public int ReadInt(long address) => _ints.GetValueOrDefault(address);
    public long ReadLong(long address) => _longs.GetValueOrDefault(address);
    public double ReadDouble(long address) => _doubles.GetValueOrDefault(address);
    public int ReadHeroHp(long shipAddress) => _heroHp.GetValueOrDefault(shipAddress);
}
