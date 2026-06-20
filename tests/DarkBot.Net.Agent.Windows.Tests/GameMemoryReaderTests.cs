using DarkBot.Net.Agent.Windows.Bridge;
using DarkBot.Net.Agent.Windows.Memory;

namespace DarkBot.Net.Agent.Windows.Tests;

public class GameMemoryReaderTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(42.9, 42)]
    [InlineData(2147483647.0, int.MaxValue)]
    [InlineData(-2147483648.0, int.MinValue)]
    [InlineData(3000000000.0, int.MaxValue)]
    public void ClampDoubleToInt_matches_java_memory_api(double input, int expected)
    {
        Assert.Equal(expected, GameMemoryReader.ClampDoubleToInt(input));
    }

    [Fact]
    public void ReadHeroHp_returns_zero_for_invalid_ship_address()
    {
        var reader = new GameMemoryReader(new FakeMemory());
        Assert.Equal(0, reader.ReadHeroHp(0));
        Assert.Equal(0, reader.ReadHeroHp(GameMemoryConstants.BadPtr));
    }
}

internal sealed class FakeMemory : INativeMemory
{
    public int ReadInt(long address) => 0;
    public long ReadLong(long address) => 0;
    public double ReadDouble(long address) => 0;
}
