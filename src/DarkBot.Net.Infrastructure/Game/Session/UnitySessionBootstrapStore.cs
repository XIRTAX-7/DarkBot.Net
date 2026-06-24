using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Infrastructure.Game.Session;

/// <summary>Хранит сессию между launch и Frida bootstrapSession RPC.</summary>
public sealed class UnitySessionBootstrapStore
{
    private readonly object _gate = new();
    private UnityWebGlSession? _pending;

    public bool HasPending
    {
        get
        {
            lock (_gate)
                return _pending is not null;
        }
    }

    public void Set(UnityWebGlSession session)
    {
        lock (_gate)
            _pending = session;
    }

    public bool TryTake(out UnityWebGlSession session)
    {
        lock (_gate)
        {
            if (_pending is null)
            {
                session = null!;
                return false;
            }

            session = _pending;
            _pending = null;
            return true;
        }
    }

    public void Clear()
    {
        lock (_gate)
            _pending = null;
    }
}
