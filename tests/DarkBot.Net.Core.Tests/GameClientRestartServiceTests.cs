using DarkBot.Net.Application.Memory;
using DarkBot.Net.Application.Tests.Helpers;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using DarkBot.Net.Infrastructure.Game.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

public sealed class GameClientRestartServiceTests
{
    [Fact]
    public async Task RestartClientAsync_InvokesLauncherRestart()
    {
        var launcher = new RecordingGameLauncher();
        var service = CreateService(launcher, hasSession: true);

        await service.RestartClientAsync(CancellationToken.None);

        Assert.Equal(1, launcher.RestartCount);
    }

    [Fact]
    public async Task TryAutoRestartAsync_RespectsCooldown()
    {
        var launcher = new RecordingGameLauncher();
        var options = Options.Create(new GameApiOptions { ClientAutoRestartCooldownSec = 60 });
        var service = CreateService(launcher, hasSession: true, options: options);

        await service.TryAutoRestartAsync("first", CancellationToken.None);
        await service.TryAutoRestartAsync("second", CancellationToken.None);

        Assert.Equal(1, launcher.RestartCount);
    }

    [Fact]
    public async Task TryAutoRestartAsync_WhenIntentionalShutdown_DoesNotRestart()
    {
        var launcher = new RecordingGameLauncher();
        var lifecycle = new GameClientLifecycle();
        lifecycle.MarkIntentionalShutdown();
        var service = CreateService(launcher, hasSession: true, lifecycle: lifecycle);

        await service.TryAutoRestartAsync("test", CancellationToken.None);

        Assert.Equal(0, launcher.RestartCount);
    }

    private static GameClientRestartService CreateService(
        RecordingGameLauncher launcher,
        bool hasSession,
        GameClientLifecycle? lifecycle = null,
        IOptions<GameApiOptions>? options = null)
    {
        lifecycle ??= new GameClientLifecycle();
        options ??= Options.Create(new GameApiOptions { ClientAutoRestartCooldownSec = 0 });

        var sessionStore = new GameSessionStore();
        if (hasSession)
        {
            sessionStore.Save(GameLaunchParameters.FromCredentials("pilot", "secret"));
        }

        var resolver = new GameLaunchSessionResolver(sessionStore, new StubCredentialStore());

        return new GameClientRestartService(
            launcher,
            resolver,
            new BotAddressRegistry(),
            lifecycle,
            new TestHostApplicationLifetime(),
            options,
            NullLogger<GameClientRestartService>.Instance);
    }

    private sealed class RecordingGameLauncher : IGameLauncherService
    {
        public int RestartCount { get; private set; }

        public IGameConnection ActiveConnection { get; } = new Fakes.FakeGameConnection();

        public Task LaunchAsync(GameLaunchParameters launch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(GameClientConnectResult.Ok(1));

        public Task<GameClientConnectResult> LaunchAndConnectAsync(
            GameLaunchParameters launch,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GameClientConnectResult.Ok(1));

        public Task<GameClientConnectResult> RestartClientAsync(
            GameLaunchParameters launch,
            CancellationToken cancellationToken = default)
        {
            RestartCount++;
            return Task.FromResult(GameClientConnectResult.Ok(1));
        }

        public void AttachProcess(long pid) { }
    }

    private sealed class StubCredentialStore : ICredentialStore
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
}
