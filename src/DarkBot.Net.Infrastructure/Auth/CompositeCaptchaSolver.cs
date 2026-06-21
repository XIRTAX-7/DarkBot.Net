namespace DarkBot.Net.Infrastructure.Auth;

public sealed class CompositeCaptchaSolver : ICaptchaSolver
{
    private readonly ManualCaptchaSolver _manual;
    private readonly ICaptchaSolver _automated;

    public CompositeCaptchaSolver(ManualCaptchaSolver manual, DarkBackpageCaptchaSolver darkBackpage)
        : this(manual, (ICaptchaSolver)darkBackpage)
    {
    }

    internal CompositeCaptchaSolver(ManualCaptchaSolver manual, ICaptchaSolver automated)
    {
        _manual = manual;
        _automated = automated;
    }

    public async Task<IReadOnlyDictionary<string, string>> SolveAsync(
        Uri pageUrl,
        string html,
        CaptchaSolveContext? context,
        CancellationToken cancellationToken = default)
    {
        if (!LoginHtmlParser.HasCaptcha(html))
            return CaptchaConstants.EmptyParams;

        var manualResult = await _manual.SolveAsync(pageUrl, html, context, cancellationToken)
            .ConfigureAwait(false);
        if (manualResult.Count > 0)
            return manualResult;

        var sidecarResult = await _automated.SolveAsync(pageUrl, html, context, cancellationToken)
            .ConfigureAwait(false);
        if (sidecarResult.Count > 0)
            return sidecarResult;

        throw new CaptchaException(
            "Captcha is required. Install dark_backpage sidecar or paste a g-recaptcha-response token manually.");
    }
}
