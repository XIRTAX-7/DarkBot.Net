using DarkBot.Net.Api.Managers;
using DarkBot.Net.Backpage;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Backpage.Tests;

public class BackpageServiceTests
{
    private sealed class TestStats : IStatsApi, ISessionMetadataProvider
    {
        public string? Sid { get; set; }
        public int UserId { get; set; }
        public string? InstanceUrl { get; set; }

        public IStatsApi.IStat GetStat(IStatsApi.IStatKey key) => throw new NotSupportedException();
        public IStatsApi.IStat RegisterStat(IStatsApi.IStatKey key) => throw new NotSupportedException();
        public void SetStatValue(IStatsApi.IStatKey key, double newValue) => throw new NotSupportedException();
        public void ResetStats() { }

        public void UpdateSession(string sid, int userId, string instanceUrl)
        {
            Sid = sid;
            UserId = userId;
            InstanceUrl = instanceUrl;
        }
    }

    [Fact]
    public void IsInstanceValid_false_without_session()
    {
        var service = new BackpageService(new TestStats(), NullLogger<BackpageService>.Instance);
        Assert.False(service.IsInstanceValid());
    }

    [Fact]
    public void SetSession_makes_instance_valid()
    {
        var stats = new TestStats();
        var service = new BackpageService(stats, NullLogger<BackpageService>.Instance);
        service.SetSession("abc", 123, new Uri("https://int1.darkorbit.com/"));
        Assert.True(service.IsInstanceValid());
        Assert.Equal("abc", stats.Sid);
    }

    [Fact]
    public void FindReloadToken_parses_body()
    {
        var service = new BackpageService(new TestStats(), NullLogger<BackpageService>.Instance);
        var token = service.FindReloadToken("reloadToken=token123\"");
        Assert.Equal("token123", token);
    }

    [Fact]
    public void CheckSidValid_returns_false_without_session()
    {
        var service = new BackpageService(new TestStats(), NullLogger<BackpageService>.Instance);
        Assert.False(service.CheckSidValid());
        Assert.Equal(BackpageSidStatus.NoSid, service.Status);
    }
}
