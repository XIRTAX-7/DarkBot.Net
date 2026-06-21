using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using DarkBot.Net.Presentation.Configuration;
using DarkBot.Net.Presentation.Services;
using DarkBot.Net.Presentation.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Presentation.Tests;

public sealed class GameAutoLaunchServiceTests
{
    [Fact]
    public async Task StopAsync_CancelsBackgroundAutoLaunch()
    {
        var sessionStore = new GameSessionStore();
        sessionStore.Save(SampleLaunch());

        var slowLaunch = new SlowGameLaunchAppService();
        var lifetime = new TestHostApplicationLifetime();
        var options = Options.Create(new GameApiOptions { BrowserApi = GameApiMode.FridaClient });

        var service = new GameAutoLaunchService(
            new FakeBackpageApi(),
            sessionStore,
            slowLaunch,
            new StubLoginAppService(),
            lifetime,
            options,
            NullLogger<GameAutoLaunchService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await slowLaunch.WaitUntilEnteredAsync(TimeSpan.FromSeconds(3));

        lifetime.SignalStopping();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(4));
        Assert.True(slowLaunch.WasCancelled || slowLaunch.IsCompleted);
    }

    [Fact]
    public async Task StartAsync_BackpageOnlyMode_DoesNotLaunch()
    {
        var slowLaunch = new SlowGameLaunchAppService();
        var lifetime = new TestHostApplicationLifetime();
        var options = Options.Create(new GameApiOptions { BrowserApi = GameApiMode.BackpageOnly });

        var service = new GameAutoLaunchService(
            new FakeBackpageApi(),
            new GameSessionStore(),
            slowLaunch,
            new StubLoginAppService(),
            lifetime,
            options,
            NullLogger<GameAutoLaunchService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        Assert.False(slowLaunch.IsCompleted);
        await service.StopAsync(CancellationToken.None);
    }

    private static GameLaunchParameters SampleLaunch() =>
        new()
        {
            InstanceUrl = "https://test.darkorbit.com/",
            Sid = "sid",
            PreloaderUrl = "https://test.darkorbit.com/preloader",
            FlashParams = new Dictionary<string, string>()
        };

    private sealed class FakeBackpageApi : IBackpageApi
    {
        public bool IsInstanceValid() => true;
        public string SidStatus => "valid";
        public string? Sid => "sid";
        public int UserId => 1;
        public Uri? InstanceUri => new("https://test.darkorbit.com/");
        public DateTimeOffset LastRequestTime => DateTimeOffset.UtcNow;
        public void UpdateLastRequestTime() { }
        public string? FindReloadToken(string body) => null;
        public void SetSession(string sid, int userId, Uri instanceUri) { }
    }

    private sealed class StubLoginAppService : ILoginAppService
    {
        public void ApplySession(LoginData loginData) { }

        public Task<LoginData> LoginWithCredentialsAsync(
            string username,
            string password,
            string? captchaToken,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not used in auto-launch session test.");

        public Task<LoginData> LoginWithSidAsync(
            string server,
            string sid,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Should not be called when session exists.");
    }

    private sealed class SlowGameLaunchAppService : IGameLaunchAppService
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile bool _cancelled;
        private volatile bool _completed;

        public bool WasCancelled => _cancelled;
        public bool IsCompleted => _completed;

        public void ScheduleLaunch(GameLaunchParameters launch) { }

        public async Task LaunchAndConnectAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default)
        {
            _entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _cancelled = true;
            }
            finally
            {
                _completed = true;
            }
        }

        public Task WaitUntilEnteredAsync(TimeSpan timeout) => _entered.Task.WaitAsync(timeout);
    }
}
