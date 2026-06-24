using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Application.Tests;

public sealed class UnitySessionBootstrapStoreTests
{
    [Fact]
    public void TryTake_ReturnsSessionOnce()
    {
        var store = new UnitySessionBootstrapStore();
        var session = new UnityWebGlSession("ru1.darkorbit.com", "sid-1", """{"ok":true}""");

        store.Set(session);

        Assert.True(store.HasPending);
        Assert.True(store.TryTake(out var taken));
        Assert.Equal(session, taken);
        Assert.False(store.TryTake(out _));
        Assert.False(store.HasPending);
    }

    [Fact]
    public void Clear_RemovesPendingSession()
    {
        var store = new UnitySessionBootstrapStore();
        store.Set(new UnityWebGlSession("ru1.darkorbit.com", "sid-1", "{}"));

        store.Clear();

        Assert.False(store.HasPending);
        Assert.False(store.TryTake(out _));
    }
}
