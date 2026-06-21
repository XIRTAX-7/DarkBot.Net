using DarkBot.Net.Application.Memory;
using DarkBot.Net.Application.Tests.Helpers;
using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
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
            sessionStore.Save(new GameLaunchParameters
            {
                InstanceUrl = "https://int1.darkorbit.com/",
                Sid = "sid",
                PreloaderUrl = "https://int1.darkorbit.com/preloader",
                FlashParams = new Dictionary<string, string>()
            });
        }

        var resolver = new GameLaunchSessionResolver(
            sessionStore,
            new StubBackpage(),
            new StubLoginAppService(),
            options);

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

    private sealed class StubBackpage : IBackpageApi
    {
        public bool IsInstanceValid() => false;
        public string SidStatus => "unknown";
        public string? Sid => null;
        public int UserId => 0;
        public Uri? InstanceUri => null;
        public DateTimeOffset LastRequestTime => DateTimeOffset.UtcNow;
        public void UpdateLastRequestTime() { }
        public string? FindReloadToken(string body) => null;
        public void SetSession(string sid, int userId, Uri instanceUri) { }
    }

    private sealed class StubLoginAppService : Contracts.ILoginAppService
    {
        public Task<Core.Models.Auth.LoginData> LoginWithCredentialsAsync(
            string username,
            string password,
            string? captchaToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Core.Models.Auth.LoginData> LoginWithSidAsync(
            string server,
            string sid,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void ApplySession(Core.Models.Auth.LoginData loginData) { }
    }
}
