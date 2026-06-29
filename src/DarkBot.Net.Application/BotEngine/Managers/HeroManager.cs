using DarkBot.Net.Core.Config.Types;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Game.Enums;
using DarkBot.Net.Core.Game.Items;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Application.BotEngine.Addresses;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Port of HeroManager — hero state from Unity bridge snapshot.</summary>
public sealed class HeroManager : IHeroApi
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameFridaProbe _frida;
    private readonly IUnityGameBridge _unityBridge;
    private readonly StarManager _starManager;
    private readonly ILogger<HeroManager> _logger;
    private readonly TrackedHealth _health = new();
    private readonly MutableLocationInfo _locationInfo = new();
    private readonly EntityInfoStub _entityInfo = new();
    private readonly Dictionary<string, object?> _metadata = new(StringComparer.Ordinal);

    private long _address;
    private ILockable? _localTarget;
    private GameMapModel _map;
    /// <summary>Активный слот корабля в игре (1/2), только из Frida.</summary>
    private HeroConfiguration _activeConfiguration = HeroConfiguration.Unknown;

    /// <summary>Целевой режим бота (OFFENSIVE/RUN/ROAM), не перезаписывает игровой слот.</summary>
    private IShipMode? _desiredShipMode;

    public HeroManager(
        BotAddressRegistry addresses,
        IGameFridaProbe frida,
        IUnityGameBridge unityBridge,
        StarManager starManager,
        ILogger<HeroManager> logger)
    {
        _addresses = addresses;
        _frida = frida;
        _unityBridge = unityBridge;
        _starManager = starManager;
        _logger = logger;
        _map = starManager.ById(-1);
    }

    internal void SetMap(GameMapModel map) => _map = map;

    public long Address => _address;
    public int HeroId { get; private set; }

    public void Tick()
    {
        if (!_frida.IsReady)
            return;

        if (!_frida.TryGetHeroSnapshot(
                out var heroId,
                out var x,
                out var y,
                out var hp,
                out var maxHp,
                out var shield,
                out var maxShield,
                out var nano,
                out var maxNano))
        {
            _address = 0;
            HeroId = 0;
            ShipType = string.Empty;
            return;
        }

        _address = _frida.HeroPointer;
        HeroId = heroId;
        _health.Update(hp, maxHp > 0 ? maxHp : hp, nano, maxNano > 0 ? maxNano : nano, shield, maxShield > 0 ? maxShield : shield);

        if (!string.IsNullOrWhiteSpace(_frida.HeroShipType))
            ShipType = _frida.HeroShipType!;

        if (_frida.HeroConfigId is 1 or 2)
            _activeConfiguration = HeroConfigurationExtensions.Of(_frida.HeroConfigId);

        if (MapLoadValidator.IsSaneCoordinate(x, y))
            _locationInfo.Update(x, y);
    }

    public bool HasMapPosition =>
        HeroId > 0 && MapLoadValidator.IsSaneCoordinate(X, Y);

    // IHeroApi + entity surface

    public IGameMap Map => _map;
    public ILockable? LocalTarget => _localTarget;

    public void SetLocalTarget(ILockable? target) => _localTarget = target;

    public HeroConfiguration ActiveConfiguration => _activeConfiguration;

    public bool IsInMode(IShipMode mode) =>
        mode.Configuration == _activeConfiguration && mode.Formation == Formation;

    public bool SetMode(IShipMode mode)
    {
        _desiredShipMode = mode;
        return IsInMode(mode);
    }

    public bool SetAttackMode(INpc? target)
    {
        if (target is null)
            return false;

        if (!_addresses.HasScreenManager)
        {
            _logger.LogDebug("SetAttackMode skipped — bridge not ready");
            return false;
        }

        return _unityBridge
            .SelectEntityAsync(target.Id, (int)target.X, (int)target.Y)
            .GetAwaiter()
            .GetResult();
    }

    public bool SetRoamMode()
    {
        _logger.LogDebug("SetRoamMode not implemented until bridge Phase 1");
        return false;
    }

    public bool SetRunMode()
    {
        _logger.LogDebug("SetRunMode not implemented until bridge Phase 1");
        return false;
    }

    public bool TriggerLaserAttack()
    {
        if (!_addresses.HasScreenManager)
        {
            _logger.LogDebug("TriggerLaserAttack skipped — bridge not ready");
            return false;
        }

        return _unityBridge.AttackAsync().GetAwaiter().GetResult();
    }

    public bool LaunchRocket()
    {
        _logger.LogDebug("LaunchRocket not implemented until bridge Phase 1");
        return false;
    }
    public ISelectableItem.ILaser Laser => EmptyLaser.Instance;
    public ISelectableItem.IRocket Rocket => EmptyRocket.Instance;

    public string ShipType { get; private set; } = string.Empty;
    public bool HasPet => false;
    public IPet? Pet => null;
    public ISelectableItem.Formation Formation => ISelectableItem.Formation.Standard;
    public bool IsInFormation(int formationId) => false;

    public int ShipId => HeroId;
    public bool IsInvisible => false;
    public bool IsBlacklisted => false;
    public void SetBlacklisted(long timeMs) { }

    public IEntity? Target => null;
    public bool IsAttacking => false;

    public ILockable.EntityLock Lock => ILockable.EntityLock.Owned;
    public IHealth Health => _health;
    public IEntityInfo EntityInfo => _entityInfo;

    public bool IsMoving() => _locationInfo.IsMoving;
    public bool IsMoving(long inTimeMs) => _locationInfo.IsMoving;
    public int Speed => (int)_locationInfo.Speed;
    public double Angle => _locationInfo.Angle;
    public double DestinationAngle => _locationInfo.Angle;
    public bool IsAiming(ILocatable other) => false;
    public ILocation? Destination => null;

    public int Id => HeroId;
    public bool IsValid => HeroId > 0 && (_health.MaxHp > 0 || HasMapPosition);
    public bool IsSelectable => false;
    public bool TrySelect(bool tryAttack) => false;    public ILocationInfo LocationInfo => _locationInfo;
    public IReadOnlyCollection<int> Effects => Array.Empty<int>();

    public void SetMetadata(string key, object? value) => _metadata[key] = value;
    public object? GetMetadata(string key) => _metadata.GetValueOrDefault(key);

    public double X => _locationInfo.X;
    public double Y => _locationInfo.Y;
}
