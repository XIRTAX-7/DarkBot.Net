using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Application.Memory;

namespace DarkBot.Net.Application.Managers;

/// <summary>Direct game actions — select, collect, useItem (port of GameAPI.DirectInteraction).</summary>
public sealed class GameDirectApi
{
    private readonly IGameConnection _game;
    private readonly BotAddressRegistry _addresses;

    public GameDirectApi(IGameConnection game, BotAddressRegistry addresses)
    {
        _game = game;
        _addresses = addresses;
    }

    public void SelectEntity(int entityId, int mapX, int mapY, int screenX = 640, int screenY = 360, int radius = 200)
    {
        if (!_addresses.HasScreenManager)
            return;

        Span<int> args = stackalloc int[8];
        args[0] = entityId;
        args[1] = mapX;
        args[2] = mapY;
        args[3] = screenX;
        args[4] = screenY;
        args[5] = screenX;
        args[6] = screenY;
        args[7] = radius;
        _game.SelectEntity(args);
    }

    public void CollectBox(long boxAddress, double x, double y)
    {
        if (!_addresses.HasScreenManager)
            return;

        _game.MoveShip(_addresses.ScreenManagerAddress, (long)x, (long)y, boxAddress);
    }

    public void UseItem(string itemId, int methodIndex, long connectionManager, long useItemCommand) =>
        _game.UseItem(_addresses.ScreenManagerAddress, itemId, methodIndex, connectionManager, useItemCommand);

    public void Refine(long refineUtilAddress, int oreId, int amount) =>
        _game.Refine(refineUtilAddress, oreId, amount);
}
