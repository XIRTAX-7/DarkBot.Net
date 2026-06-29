using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Interfaces.Game;
using EntityInfoStub = DarkBot.Net.Core.Entities.EntityInfoStub;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Box из Frida snapshot — сбор через bridge RPC collectTo.</summary>
public sealed class BoxEntity(IUnityGameBridge bridge, IBoxInfo info) : IBox
{
    private readonly EntityInfoStub _entityInfo = new();
    private readonly TrackedHealth _health = new();
    private bool _isCollected;

    public required int Id { get; init; }
    public required MutableLocationInfo Location { get; init; }
    public string Hash { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public IBoxInfo Info { get; } = info;
    public bool IsCollected => _isCollected;
    public int Retries { get; private set; }
    public DateTimeOffset? CollectedUntil { get; private set; }
    public IReadOnlyCollection<int> Effects { get; init; } = [];

    public bool IsValid => Id > 0 && Location.IsInitialized;
    public bool IsSelectable => true;
    public IEntityInfo EntityInfo => _entityInfo;
    ILocationInfo IEntity.LocationInfo => Location;
    public double X => Location.X;
    public double Y => Location.Y;

    public bool TrySelect(bool tryAttack) =>
        bridge.SelectEntityAsync(Id, (int)X, (int)Y).GetAwaiter().GetResult();

    public bool TryCollect()
    {
        if (_isCollected)
            return false;

        Retries++;
        var ok = bridge.CollectBoxAsync(Id, (int)X, (int)Y).GetAwaiter().GetResult();
        if (ok)
            SetCollected();

        return ok;
    }

    public void SetCollected()
    {
        _isCollected = true;
        CollectedUntil = DateTimeOffset.UtcNow.AddSeconds(3);
    }

    public void SetMetadata(string key, object? value) { }
    public object? GetMetadata(string key) => null;
}
