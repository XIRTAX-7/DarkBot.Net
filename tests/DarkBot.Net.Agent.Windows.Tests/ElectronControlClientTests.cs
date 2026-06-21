using System.Buffers.Binary;
using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Infrastructure.Game.Tests;

public sealed class ElectronControlClientTests
{
    [Fact]
    public void BuildRequest_UsesBigEndianOpcode()
    {
        var request = InvokeBuildRequest(ElectronControlOpcodes.Reload, null);

        Assert.Equal(2, request.Length);
        Assert.Equal(ElectronControlOpcodes.Reload, BinaryPrimitives.ReadInt16BigEndian(request));
    }

    [Fact]
    public void BuildRequest_AppendsPayloadAfterOpcode()
    {
        var payload = new byte[] { 0, 0, 0, 100, 0, 0, 0, 80 };
        var request = InvokeBuildRequest(ElectronControlOpcodes.SetSize, payload);

        Assert.Equal(10, request.Length);
        Assert.Equal(ElectronControlOpcodes.SetSize, BinaryPrimitives.ReadInt16BigEndian(request));
        Assert.Equal(payload, request[2..]);
    }

    [Fact]
    public void GamePacketReader_FlagsInvalidSessionNames()
    {
        var reader = new GamePacketReader();
        GamePacketMessage? invalid = null;
        reader.InvalidSessionDetected += msg => invalid = msg;

        reader.HandleMessage("""{"type":"in","id":1,"name":"LogoutRequest","data":{}}""");

        Assert.NotNull(invalid);
        Assert.Equal("LogoutRequest", invalid!.Name);
    }

    private static byte[] InvokeBuildRequest(short opcode, byte[]? payload)
    {
        var method = typeof(ElectronControlClient).GetMethod(
            "BuildRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (byte[])method!.Invoke(null, [opcode, payload])!;
    }
}
