using DarkBot.Net.Agent.Windows.Bridge;
using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Agent.Windows.Memory;

namespace DarkBot.Net.Core.Memory;

public sealed class GameMemoryAccess : IGameMemoryAccess
{
    private readonly NativeGameBridge _bridge;
    private readonly IGameConnection _game;

    public GameMemoryAccess(NativeGameBridge bridge, IGameConnection game)
    {
        _bridge = bridge;
        _game = game;
    }

    public int ReadInt(long address) =>
        UseKekka() ? _bridge.Kekka.ReadInt(address) : _game.ReadInt(address);

    public long ReadLong(long address) =>
        UseKekka() ? _bridge.Kekka.ReadLong(address) : _game.ReadLong(address);

    public double ReadDouble(long address) =>
        UseKekka() ? _bridge.Kekka.ReadDouble(address) : _game.ReadDouble(address);

    public int ReadHeroHp(long shipAddress) => _bridge.ReadHeroHp(shipAddress);

    private bool UseKekka() => false;

    private bool UseAttachedMemory() => _game.Mode == GameApiMode.FridaClient;
}
