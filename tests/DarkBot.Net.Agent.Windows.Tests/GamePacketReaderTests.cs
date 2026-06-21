using System.Buffers.Binary;
using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Infrastructure.Game.Tests;

public sealed class GamePacketReaderTests
{
    [Fact]
    public void HandleMessage_UpdatesLastPacketAt()
    {
        var reader = new GamePacketReader();

        reader.HandleMessage("""{"type":"in","id":42,"name":"MoveCommand","data":{}}""");

        Assert.NotNull(reader.LastPacketAt);
    }

    [Fact]
    public void HandleMessage_IgnoresMalformedJson()
    {
        var reader = new GamePacketReader();
        var called = false;
        reader.PacketReceived += _ => called = true;

        reader.HandleMessage("not-json");

        Assert.False(called);
        Assert.Null(reader.LastPacketAt);
    }
}
