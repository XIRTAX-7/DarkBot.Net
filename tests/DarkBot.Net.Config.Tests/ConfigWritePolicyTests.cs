using DarkBot.Net.Core.Config;
using DarkBot.Net.Infrastructure.Config;

namespace DarkBot.Net.Infrastructure.Config.Tests;

public class ConfigWritePolicyTests
{
    private readonly ConfigWritePolicy _policy = new();

    [Theory]
    [InlineData(ProfileOwner.User, ConfigActor.User, true)]
    [InlineData(ProfileOwner.User, ConfigActor.Ai, false)]
    [InlineData(ProfileOwner.Ai, ConfigActor.Ai, true)]
    [InlineData(ProfileOwner.Ai, ConfigActor.User, false)]
    public void CanWrite_RespectsOwnerAndActor(ProfileOwner owner, ConfigActor actor, bool expected) =>
        Assert.Equal(expected, _policy.CanWrite("profile", owner, actor));

    [Fact]
    public void EnsureCanWrite_UserActorOnAiProfile_Throws()
    {
        var act = () => _policy.EnsureCanWrite("ai-pve", ProfileOwner.Ai, ConfigActor.User);
        Assert.Throws<UnauthorizedAccessException>(act);
    }
}
