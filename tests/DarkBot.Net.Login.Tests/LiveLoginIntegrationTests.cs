using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Auth.Tests;

/// <summary>Run with DARKBOT_LIVE_LOGIN=1 and DARKBOT_LOGIN_USER / DARKBOT_LOGIN_PASS env vars.</summary>
public class LiveLoginIntegrationTests
{
    [Fact]
    public async Task LoginWithCredentials_live_when_enabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DARKBOT_LIVE_LOGIN"), "1", StringComparison.Ordinal))
            return;

        var username = Environment.GetEnvironmentVariable("DARKBOT_LOGIN_USER");
        var password = Environment.GetEnvironmentVariable("DARKBOT_LOGIN_PASS");
        var captchaToken = Environment.GetEnvironmentVariable("DARKBOT_LOGIN_CAPTCHA");

        Assert.False(string.IsNullOrWhiteSpace(username), "Set DARKBOT_LOGIN_USER.");
        Assert.False(string.IsNullOrWhiteSpace(password), "Set DARKBOT_LOGIN_PASS.");

        var options = Options.Create(new LoginOptions
        {
            BackpageSidecarPath = Environment.GetEnvironmentVariable("DARKBOT_BACKPAGE_SIDECAR")
                                  ?? "./sidecars/backpage/dark_backpage.exe"
        });

        var locator = new BackpageSidecarLocator(options, NullLogger<BackpageSidecarLocator>.Instance);
        var solver = new CompositeCaptchaSolver(
            new ManualCaptchaSolver(),
            new DarkBackpageCaptchaSolver(locator, NullLogger<DarkBackpageCaptchaSolver>.Instance));

        var service = new LoginService(solver, NullLogger<LoginService>.Instance);

        var loginData = await service.LoginWithCredentialsAsync(
            username!,
            password!,
            string.IsNullOrWhiteSpace(captchaToken) ? null : captchaToken);

        Assert.False(string.IsNullOrWhiteSpace(loginData.Sid));
        Assert.NotNull(loginData.InstanceUri);
        Assert.True(loginData.UserId > 0);
        Assert.False(loginData.IsNotInitialized);
    }
}
