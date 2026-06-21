using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Auth;

public sealed class DarkBackpageCaptchaSolver : ICaptchaSolver
{
    private const string CaptchaResultPrefix = "[captchaResult]";
    private const string CaptchaFailedMarker = "[captchaFailed]";

    private readonly BackpageSidecarLocator _locator;
    private readonly ILogger<DarkBackpageCaptchaSolver> _logger;

    public DarkBackpageCaptchaSolver(BackpageSidecarLocator locator, ILogger<DarkBackpageCaptchaSolver> logger)
    {
        _locator = locator;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, string>> SolveAsync(
        Uri pageUrl,
        string html,
        CaptchaSolveContext? context,
        CancellationToken cancellationToken = default)
    {
        var siteKey = LoginHtmlParser.GetCaptchaSiteKey(html);
        if (siteKey is null)
            return CaptchaConstants.EmptyParams;

        using var process = _locator.StartProcess(
            "--captcha", siteKey,
            "--exit", "30000",
            "--url", pageUrl.ToString());

        var token = await ReadCaptchaResultAsync(process, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
            return CaptchaConstants.EmptyParams;

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["g-recaptcha-response"] = token,
            ["h-captcha-response"] = token
        };
    }

    internal static async Task<string?> ReadCaptchaResultAsync(Process process, CancellationToken cancellationToken)
    {
        while (!process.HasExited)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;

            if (line.Contains(CaptchaFailedMarker, StringComparison.Ordinal))
                return null;

            var prefixIndex = line.IndexOf(CaptchaResultPrefix, StringComparison.Ordinal);
            if (prefixIndex < 0)
                continue;

            var token = line[(prefixIndex + CaptchaResultPrefix.Length)..].Trim();
            return string.IsNullOrEmpty(token) ? null : token;
        }

        return null;
    }

    public static string? ParseCaptchaLine(string line)
    {
        if (line.Contains(CaptchaFailedMarker, StringComparison.Ordinal))
            return null;

        var prefixIndex = line.IndexOf(CaptchaResultPrefix, StringComparison.Ordinal);
        if (prefixIndex < 0)
            return null;

        var token = line[(prefixIndex + CaptchaResultPrefix.Length)..].Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }
}
