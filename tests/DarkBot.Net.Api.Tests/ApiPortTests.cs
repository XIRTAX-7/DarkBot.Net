using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Utils;

namespace DarkBot.Net.Core.Tests;

public class ApiPortTests
{
    [Fact]
    public void GameLocation_distance_matches_java_semantics()
    {
        var a = (ILocatable)GameLocation.Of(0, 0);
        var b = GameLocation.Of(3, 4);
        Assert.Equal(5, a.DistanceTo(b), precision: 5);
    }

    [Fact]
    public void BotTimer_starts_inactive()
    {
        var timer = BotTimer.Create();
        Assert.True(timer.IsInactive);
        Assert.False(timer.IsActive);
    }

    [Fact]
    public void BotTimer_activate_makes_active()
    {
        var timer = BotTimer.Create();
        timer.Activate(1000);
        Assert.True(timer.IsActive);
    }
}
