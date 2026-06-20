using DarkBot.Net.Api.Config.Types;
using DarkBot.Net.Api.Game;
using DarkBot.Net.Api.Game.Entities;
using DarkBot.Net.Api.Game.Enums;
using DarkBot.Net.Api.Game.Items;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Core.Entities;
using DarkBot.Net.Core.Memory;

namespace DarkBot.Net.Core.Managers;

/// <summary>Port of HeroManager — hero state from native memory; mode/keybinds in Phase 3+.</summary>
public sealed class HeroManager : IHeroApi
{
    private const int HeroStaticOffset = 240;

    private readonly BotAddressRegistry _addresses;
    private readonly IGameMemoryAccess _memory;
    private readonly StarManager _starManager;
    private readonly TrackedHealth _health = new();
    private readonly MutableLocationInfo _locationInfo = new();
    private readonly EntityInfoStub _entityInfo = new();
    private readonly Dictionary<string, object?> _metadata = new(StringComparer.Ordinal);

    private long _staticAddress;
    private long _address;
    private ILockable? _localTarget;
    private GameMapModel _map;
    private HeroConfiguration _configuration = HeroConfiguration.Unknown;

    public HeroManager(BotAddressRegistry addresses, IGameMemoryAccess memory, StarManager starManager)
    {
        _addresses = addresses;
        _memory = memory;
        _starManager = starManager;
        _map = starManager.ById(-1);
        _addresses.ScreenManagerAddressChanged += OnScreenManagerAddressChanged;
    }

    internal void SetMap(GameMapModel map) => _map = map;

    public long Address => _address;
    public int HeroId { get; private set; }

    public void Tick()
    {
        if (!_addresses.HasScreenManager)
            return;

        var address = _memory.ReadLong(_staticAddress);
        if (_address != address)
            UpdateAddress(address);

        Update();
    }

    private void UpdateAddress(long address)
    {
        _address = address;
        if (address == 0)
            return;

        HeroId = _memory.ReadInt(address + 56);
        var hp = _memory.ReadHeroHp(address);
        _health.Update(hp);
    }

    private void Update()
    {
        if (_address == 0)
            return;

        var hp = _memory.ReadHeroHp(_address);
        _health.Update(hp);
    }

    private void OnScreenManagerAddressChanged(long screenManagerAddress)
    {
        _staticAddress = screenManagerAddress + HeroStaticOffset;
    }

    // IHeroApi + entity surface

    public IGameMap Map => _map;
    public ILockable? LocalTarget => _localTarget;

    public void SetLocalTarget(ILockable? target) => _localTarget = target;

    public HeroConfiguration ActiveConfiguration => _configuration;

    public bool IsInMode(IShipMode mode) =>
        mode.Configuration == _configuration;

    public bool SetMode(IShipMode mode)
    {
        _configuration = mode.Configuration;
        return IsInMode(mode);
    }

    public bool SetAttackMode(INpc? target) => false;
    public bool SetRoamMode() => false;
    public bool SetRunMode() => false;
    public bool TriggerLaserAttack() => false;
    public bool LaunchRocket() => false;
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
    public bool IsValid => _address != 0;
    public bool IsSelectable => false;
    public bool TrySelect(bool tryAttack) => false;
    public ILocationInfo LocationInfo => _locationInfo;
    public IReadOnlyCollection<int> Effects => Array.Empty<int>();

    public void SetMetadata(string key, object? value) => _metadata[key] = value;
    public object? GetMetadata(string key) => _metadata.GetValueOrDefault(key);

    public double X => _locationInfo.X;
    public double Y => _locationInfo.Y;
}
