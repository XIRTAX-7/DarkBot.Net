using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Core.Entities;

namespace DarkBot.Net.Application.Tests;

public class BoxEntityTests
{
    [Fact]
    public void TryCollect_calls_collect_box_async_on_bridge()
    {
        var bridge = new FakeGameConnection();
        var box = new BoxEntity(bridge)
        {
            Id = 42,
            Location = new MutableLocationInfo(),
        };
        box.Location.Update(100, 200);

        Assert.True(box.TryCollect());
        Assert.Contains((42, 100, 200), bridge.CollectBoxCalls);
        Assert.True(box.IsCollected);
    }
}
