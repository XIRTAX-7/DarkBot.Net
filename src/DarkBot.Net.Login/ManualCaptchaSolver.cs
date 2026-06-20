namespace DarkBot.Net.Login;

public sealed class ManualCaptchaSolver : ICaptchaSolver
{
    public Task<IReadOnlyDictionary<string, string>> SolveAsync(
        Uri pageUrl,
        string html,
        CaptchaSolveContext? context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context?.ManualToken))
            return Task.FromResult(CaptchaConstants.EmptyParams);

        var token = context.ManualToken.Trim();
        IReadOnlyDictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["g-recaptcha-response"] = token,
            ["h-captcha-response"] = token
        };
        return Task.FromResult(result);
    }
}
