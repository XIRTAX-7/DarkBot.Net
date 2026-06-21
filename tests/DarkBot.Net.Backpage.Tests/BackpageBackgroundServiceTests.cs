using DarkBot.Net.Core.Managers;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Infrastructure.Auth.Tests;

public sealed class BackpageBackgroundServiceTests
{
    [Fact]
    public async Task StopAsync_ExitsWithoutTaskCanceledException()
    {
        var backpage = new BackpageService(new TestStats(), NullLogger<BackpageService>.Instance);
        var service = new BackpageBackgroundService(backpage, NullLogger<BackpageBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await service.StopAsync(CancellationToken.None);
    }

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
}
