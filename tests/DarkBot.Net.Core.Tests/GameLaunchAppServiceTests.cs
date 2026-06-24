using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Application.Services.Game;
using DarkBot.Net.Application.Tests.Fakes;
using DarkBot.Net.Application.Tests.Helpers;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public sealed class GameLaunchAppServiceTests
{
    [Fact]
    public async Task LaunchAndConnectAsync_CancellationTokenCancelsLongConnect()
    {
        using var cts = new CancellationTokenSource();
        var launcher = new BlockingGameLauncher();
        var game = new FakeGameConnection { IsValid = false, Phase = GameConnectionPhase.NotStarted };
        var lifetime = new TestHostApplicationLifetime();

        var service = new GameLaunchAppService(
            launcher,
            game,
            lifetime,
            NullLogger<GameLaunchAppService>.Instance);

        var launchTask = service.LaunchAndConnectAsync(SampleLaunch(), cts.Token);

        await launcher.WaitUntilConnectEnteredAsync(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();

        await launchTask;
        Assert.True(launcher.WasCancelled);
    }

    [Fact]
    public async Task ScheduleLaunch_UsesApplicationStoppingToken()
    {
        var launcher = new BlockingGameLauncher();
        var game = new FakeGameConnection { IsValid = false, Phase = GameConnectionPhase.NotStarted };
        var lifetime = new TestHostApplicationLifetime();

        var service = new GameLaunchAppService(
            launcher,
            game,
            lifetime,
            NullLogger<GameLaunchAppService>.Instance);

        service.ScheduleLaunch(SampleLaunch());

        await launcher.WaitUntilConnectEnteredAsync(TimeSpan.FromSeconds(3));
        lifetime.SignalStopping();

        await AsyncWaitHelpers.WaitUntilAsync(
            () => launcher.WasCancelled,
            TimeSpan.FromSeconds(3));
    }

    private static GameLaunchParameters SampleLaunch() =>
        GameLaunchParameters.FromCredentials("pilot", "secret");

    private sealed class BlockingGameLauncher : IGameLauncherService
    {
        private readonly TaskCompletionSource _connectEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile bool _cancelled;

        public IGameConnection ActiveConnection { get; } = new FakeGameConnection();

        public bool WasCancelled => _cancelled;

        public Task LaunchAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            _connectEntered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return GameClientConnectResult.Ok(1);
            }
            catch (OperationCanceledException)
            {
                _cancelled = true;
                throw;
            }
        }

        public Task<GameClientConnectResult> LaunchAndConnectAsync(
            GameLaunchParameters launch,
            CancellationToken cancellationToken = default)
        {
            return ConnectAsync(cancellationToken);
        }

        public Task<GameClientConnectResult> RestartClientAsync(
            GameLaunchParameters launch,
            CancellationToken cancellationToken = default) =>
            ConnectAsync(cancellationToken);

        public void AttachProcess(long pid) { }

        public Task WaitUntilConnectEnteredAsync(TimeSpan timeout) =>
            _connectEntered.Task.WaitAsync(timeout);
    }
}
