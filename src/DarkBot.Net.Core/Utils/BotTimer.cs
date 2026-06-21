namespace DarkBot.Net.Core.Utils;

/// <summary>Port of eu.darkbot.util.Timer — bomb-timer style cooldown helper.</summary>
public sealed class BotTimer
{
    private long _time;
    private readonly long _defaultFuse;
    private readonly long _randomRange;

    public static BotTimer Create() => new(-1, -1);

    public static BotTimer Create(long defaultFuseMs)
    {
        if (defaultFuseMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultFuseMs));

        return new(defaultFuseMs, -1);
    }

    public static BotTimer CreateRandom(long randomRangeMs)
    {
        if (randomRangeMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(randomRangeMs));

        return new(-1, randomRangeMs);
    }

    private BotTimer(long defaultFuse, long randomRange)
    {
        _defaultFuse = defaultFuse;
        _randomRange = randomRange;
    }

    public bool TryActivate(long fuseMs)
    {
        if (IsActive) return false;
        SetTime(fuseMs);
        return true;
    }

    public bool TryActivate()
    {
        if (_defaultFuse <= 0)
            throw new InvalidOperationException("Timer has no default fuse");

        return TryActivate(_defaultFuse);
    }

    public bool Activate(long fuseMs)
    {
        var wasInactive = IsInactive;
        SetTime(fuseMs);
        return wasInactive;
    }

    public bool Activate()
    {
        if (_defaultFuse <= 0)
            throw new InvalidOperationException("Timer has no default fuse");

        return Activate(_defaultFuse);
    }

    public bool IsActive => _time >= Environment.TickCount64;
    public bool IsInactive => _time < Environment.TickCount64;
    public bool IsArmed => _time > 0;

    public bool TryDisarm()
    {
        if (!IsArmed || !IsInactive) return false;
        Disarm();
        return true;
    }

    public void Disarm() => _time = 0;

    public long RemainingFuseMs => _time - Environment.TickCount64;

    private void SetTime(long fuseMs)
    {
        var random = _randomRange <= 0 ? 0 : Random.Shared.NextInt64(_randomRange);
        _time = Environment.TickCount64 + fuseMs + random;
    }
}
