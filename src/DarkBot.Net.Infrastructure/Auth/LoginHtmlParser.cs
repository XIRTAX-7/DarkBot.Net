using System.Net;
using System.Text.RegularExpressions;

namespace DarkBot.Net.Infrastructure.Auth;

public static partial class LoginHtmlParser
{
    public const string CaptchaMarker = "class=\"bgcdw_captcha\"";

    public static string GetLoginUrl(string html)
    {
        var match = LoginFormActionRegex().Match(html);
        if (!match.Success)
            throw new LoginException("Could not parse login form from DarkOrbit front page.");

        return match.Groups[1].Value.Replace("&amp;", "&", StringComparison.Ordinal);
    }

    public static bool HasCaptcha(string html) =>
        html.Contains(CaptchaMarker, StringComparison.Ordinal);

    public static string? GetCaptchaSiteKey(string html)
    {
        var match = CaptchaSiteKeyRegex().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static (string PreloaderUrl, string ParamsJson) ParseFlashEmbed(string flashEmbedLine)
    {
        var match = FlashEmbedDataRegex().Match(flashEmbedLine);
        if (!match.Success)
            throw new WrongCredentialsException("Could not parse flash embed parameters.");

        return (match.Groups[1].Value, match.Groups[2].Value);
    }

    public static string? FindFlashEmbedLine(string html)
    {
        foreach (var line in html.Split('\n'))
        {
            if (line.Contains("flashembed(", StringComparison.Ordinal))
                return line;
        }

        return null;
    }

    public static (string Sid, string Domain) ExtractDosidCookie(CookieCollection cookies)
    {
        foreach (Cookie cookie in cookies)
        {
            if (!cookie.Name.Equals("dosid", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!DosidDomainRegex().IsMatch(cookie.Domain))
                continue;

            var domain = cookie.Domain.TrimStart('.');
            return (cookie.Value, domain);
        }

        throw new WrongCredentialsException();
    }

    [GeneratedRegex("\"bgcdw_login_form\" action=\"(.*)\"")]
    private static partial Regex LoginFormActionRegex();

    [GeneratedRegex("data-sitekey=\"([^\"]+)\"")]
    private static partial Regex CaptchaSiteKeyRegex();

    [GeneratedRegex("\"src\": \"([^\"]*)\".*}, (\\{.*})")]
    private static partial Regex FlashEmbedDataRegex();

    [GeneratedRegex(@".*\d+.*")]
    private static partial Regex DosidDomainRegex();
}
