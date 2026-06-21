using DarkBot.Net.Core.Game;

namespace DarkBot.Net.Application.Entities;

public sealed class TrackedHealth : IHealth
{
    private int _hp;
    private int _hull;
    private int _shield;
    private int _maxHp;
    private int _maxHull;
    private int _maxShield;
    private int _lastHp;
    private int _lastHull;
    private int _lastShield;
    private long _lastChangeMs;

    public int Hp => _hp;
    public int Hull => _hull;
    public int Shield => _shield;
    public int MaxHp => _maxHp;
    public int MaxHull => _maxHull;
    public int MaxShield => _maxShield;

    public void Update(int hp, int maxHp = 0, int hull = 0, int maxHull = 0, int shield = 0, int maxShield = 0)
    {
        if (hp != _hp || hull != _hull || shield != _shield)
            _lastChangeMs = Environment.TickCount64;

        _lastHp = _hp;
        _lastHull = _hull;
        _lastShield = _shield;
        _hp = hp;
        _maxHp = maxHp > 0 ? maxHp : Math.Max(_maxHp, hp);
        _hull = hull;
        _maxHull = maxHull > 0 ? maxHull : Math.Max(_maxHull, hull);
        _shield = shield;
        _maxShield = maxShield > 0 ? maxShield : Math.Max(_maxShield, shield);
    }

    public bool HpDecreasedIn(int timeMs) => _hp < _lastHp && ChangedWithin(timeMs);
    public bool HullDecreasedIn(int timeMs) => _hull < _lastHull && ChangedWithin(timeMs);
    public bool ShieldDecreasedIn(int timeMs) => _shield < _lastShield && ChangedWithin(timeMs);
    public bool HpIncreasedIn(int timeMs) => _hp > _lastHp && ChangedWithin(timeMs);
    public bool HullIncreasedIn(int timeMs) => _hull > _lastHull && ChangedWithin(timeMs);
    public bool ShieldIncreasedIn(int timeMs) => _shield > _lastShield && ChangedWithin(timeMs);

    private bool ChangedWithin(int timeMs) => Environment.TickCount64 - _lastChangeMs <= timeMs;
}
