using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.Services.Game;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Game.Session;
using DarkBot.Net.Presentation.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Presentation.Tests;

public sealed class GameAutoLaunchHostedServiceTests
{
    [Fact]
    public async Task StopAsync_CancelsBackgroundAutoLaunch()
    {
        var sessionStore = new GameSessionStore();
        sessionStore.Save(SampleLaunch());

        var slowLaunch = new SlowGameLaunchAppService();
        var lifetime = new TestHostApplicationLifetime();

        var service = new GameAutoLaunchHostedService(
            new FakeCredentialStore(),
            sessionStore,
            slowLaunch,
            lifetime,
            NullLogger<GameAutoLaunchHostedService>.Instance);

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
    public async Task StartAsync_WithoutCredentials_DoesNotLaunch()
    {
        var slowLaunch = new SlowGameLaunchAppService();
        var lifetime = new TestHostApplicationLifetime();

        var service = new GameAutoLaunchHostedService(
            new FakeCredentialStore(),
            new GameSessionStore(),
            slowLaunch,
            lifetime,
            NullLogger<GameAutoLaunchHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        Assert.False(slowLaunch.IsCompleted);
        await service.StopAsync(CancellationToken.None);
    }

    private static GameLaunchParameters SampleLaunch() =>
        GameLaunchParameters.FromCredentials("pilot", "secret");

    private sealed class FakeCredentialStore : ICredentialStore
    {
        public bool HasSaved => false;

        public void Clear() { }

        public void Save(SavedCredentials credentials) { }

        public bool TryLoad(out SavedCredentials credentials)
        {
            credentials = null!;
            return false;
        }
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
