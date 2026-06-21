using DarkBot.Net.Agent.Windows.Bridge;
using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Api.Game;

namespace DarkBot.Net.Core.Memory;

public sealed class GameMemoryAccess : IGameMemoryAccess
{
    private readonly NativeGameBridge _bridge;
    private readonly IGameConnection _game;
    private readonly IGameFridaProbe _frida;

    public GameMemoryAccess(NativeGameBridge bridge, IGameConnection game, IGameFridaProbe frida)
    {
        _bridge = bridge;
        _game = game;
        _frida = frida;
    }

    public int ReadInt(long address) =>
        UseKekka() ? _bridge.Kekka.ReadInt(address) : _game.ReadInt(address);

    public long ReadLong(long address) =>
        UseKekka() ? _bridge.Kekka.ReadLong(address) : _game.ReadLong(address);

    public double ReadDouble(long address) =>
        UseKekka() ? _bridge.Kekka.ReadDouble(address) : _game.ReadDouble(address);

    public int ReadHeroHp(long shipAddress)
    {
        if (_frida.IsReady && _frida.TryGetHeroSnapshot(out _, out _, out _, out var hp, out _))
            return hp;

        return _bridge.ReadHeroHp(shipAddress);
    }

    private bool UseKekka() => false;
}
