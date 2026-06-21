using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Infrastructure.Auth.Tests;

public class LoginDataTests
{
    [Fact]
    public void SetPreloader_parses_user_id()
    {
        var data = new LoginData();
        data.SetPreloader("https://ru1.darkorbit.com/preloader.swf", """{"userID":"4242"}""");

        Assert.Equal(4242, data.UserId);
        Assert.False(data.IsNotInitialized);
    }

    [Fact]
    public void SetSid_builds_instance_uri()
    {
        var data = new LoginData();
        data.SetSid("sid-value", "ru1");

        Assert.Equal("ru1.darkorbit.com", data.InstanceHost);
        Assert.Equal(new Uri("https://ru1.darkorbit.com/"), data.InstanceUri);
    }
}
