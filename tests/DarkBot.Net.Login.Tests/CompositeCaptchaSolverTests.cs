namespace DarkBot.Net.Login.Tests;

public class CompositeCaptchaSolverTests
{
    [Fact]
    public async Task SolveAsync_prefers_manual_token_over_sidecar()
    {
        var composite = new CompositeCaptchaSolver(
            new ManualCaptchaSolver(),
            new ThrowingCaptchaSolver());

        var html = """<div class="bgcdw_captcha" data-sitekey="key"></div>""";
        var result = await composite.SolveAsync(
            new Uri("https://www.darkorbit.com/"),
            html,
            new CaptchaSolveContext { ManualToken = "manual-token" });

        Assert.Equal("manual-token", result["g-recaptcha-response"]);
        Assert.Equal("manual-token", result["h-captcha-response"]);
    }

    [Fact]
    public async Task SolveAsync_returns_empty_when_no_captcha()
    {
        var composite = new CompositeCaptchaSolver(new ManualCaptchaSolver(), new EmptyCaptchaSolver());

        var result = await composite.SolveAsync(
            new Uri("https://www.darkorbit.com/"),
            "<html></html>",
            null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SolveAsync_throws_when_captcha_unsolved()
    {
        var composite = new CompositeCaptchaSolver(new ManualCaptchaSolver(), new EmptyCaptchaSolver());
        var html = """<div class="bgcdw_captcha" data-sitekey="key"></div>""";

        await Assert.ThrowsAsync<CaptchaException>(() => composite.SolveAsync(
            new Uri("https://www.darkorbit.com/"),
            html,
            null));
    }

    private sealed class ThrowingCaptchaSolver : ICaptchaSolver
    {
        public Task<IReadOnlyDictionary<string, string>> SolveAsync(
            Uri pageUrl,
            string html,
            CaptchaSolveContext? context,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Sidecar should not run when manual token is provided.");
    }

    private sealed class EmptyCaptchaSolver : ICaptchaSolver
    {
        public Task<IReadOnlyDictionary<string, string>> SolveAsync(
            Uri pageUrl,
            string html,
            CaptchaSolveContext? context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }
}
