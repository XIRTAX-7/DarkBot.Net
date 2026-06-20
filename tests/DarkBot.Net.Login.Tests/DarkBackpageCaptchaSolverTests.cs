namespace DarkBot.Net.Login.Tests;

public class DarkBackpageCaptchaSolverTests
{
    [Fact]
    public void ParseCaptchaLine_reads_result_token()
    {
        var token = DarkBackpageCaptchaSolver.ParseCaptchaLine("[captchaResult]token-abc");

        Assert.Equal("token-abc", token);
    }

    [Fact]
    public void ParseCaptchaLine_returns_null_on_failure_marker()
    {
        Assert.Null(DarkBackpageCaptchaSolver.ParseCaptchaLine("[captchaFailed]"));
    }
}
