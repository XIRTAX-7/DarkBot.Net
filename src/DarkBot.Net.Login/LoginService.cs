using System.Net;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Api.Utils.Http;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Login;

public sealed class LoginService
{
    private const string FrontPageDomain = "www";

    private readonly ICaptchaSolver _captchaSolver;
    private readonly ILogger<LoginService> _logger;

    public LoginService(ICaptchaSolver captchaSolver, ILogger<LoginService> logger)
    {
        _captchaSolver = captchaSolver;
        _logger = logger;
    }

    public async Task<LoginData> LoginWithCredentialsAsync(
        string username,
        string password,
        string? captchaToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting credential login for user {Username}", username);

        var loginData = new LoginData();
        loginData.SetCredentials(username, password);
        await UsernameLoginAsync(loginData, captchaToken, cancellationToken).ConfigureAwait(false);
        await FindPreloaderAsync(loginData, cancellationToken).ConfigureAwait(false);

        LogLoginSuccess(loginData, "credentials");
        return loginData;
    }

    public async Task<LoginData> LoginWithSidAsync(
        string server,
        string sid,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting SID login for server {Server}, sidSuffix={SidSuffix}",
            server,
            sid.Length >= 4 ? sid[^4..] : sid);

        var loginData = new LoginData();
        loginData.SetSid(sid.Trim(), server.Trim());
        await FindPreloaderAsync(loginData, cancellationToken).ConfigureAwait(false);

        LogLoginSuccess(loginData, "sid");
        return loginData;
    }

    public async Task FindPreloaderAsync(LoginData loginData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginData.Sid) || string.IsNullOrWhiteSpace(loginData.InstanceHost))
            throw new LoginException("SID and server are required before loading the spacemap.");

        var url = $"https://{loginData.InstanceHost}/indexInternal.es?action=internalMapRevolution";
        _logger.LogDebug("Loading spacemap preloader from {Url}", url);

        using var client = CreateRedirectDisabledClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", BotHttpClient.DefaultUserAgent);
        request.Headers.TryAddWithoutValidation("Cookie", "dosid=" + loginData.Sid);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Spacemap preloader response: HTTP {StatusCode}, length={Length}",
            (int)response.StatusCode,
            html.Length);

        if ((int)response.StatusCode is >= 400)
        {
            throw new WrongCredentialsException(
                $"Spacemap page returned HTTP {(int)response.StatusCode}. SID may be expired.");
        }

        var flashLine = LoginHtmlParser.FindFlashEmbedLine(html)
                        ?? throw new WrongCredentialsException("Could not find flash embed on spacemap page.");

        var (preloaderUrl, paramsJson) = LoginHtmlParser.ParseFlashEmbed(flashLine);
        loginData.SetPreloader(preloaderUrl, paramsJson);

        _logger.LogInformation(
            "Preloader resolved: userId={UserId}, preloader={PreloaderUrl}",
            loginData.UserId,
            preloaderUrl);
    }

    public async Task UsernameLoginAsync(
        LoginData loginData,
        string? captchaToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginData.Username) || string.IsNullOrWhiteSpace(loginData.Password))
            throw new LoginException("Username and password are required.");

        await UsernameLoginAsync(loginData, FrontPageDomain, captchaToken, cancellationToken).ConfigureAwait(false);
    }

    public void ApplySession(LoginData loginData, IBackpageApi backpage, ISessionMetadataProvider session)
    {
        if (loginData.InstanceUri is null || string.IsNullOrWhiteSpace(loginData.Sid) || loginData.UserId <= 0)
            throw new LoginException("Login did not produce a valid session.");

        _logger.LogInformation(
            "Applying session: userId={UserId}, instance={Instance}",
            loginData.UserId,
            loginData.InstanceHost);

        session.UpdateSession(loginData.Sid, loginData.UserId, loginData.InstanceUri.ToString());
        backpage.SetSession(loginData.Sid, loginData.UserId, loginData.InstanceUri);
    }

    private async Task UsernameLoginAsync(
        LoginData loginData,
        string domain,
        string? captchaToken,
        CancellationToken cancellationToken)
    {
        var frontPageUrl = new Uri($"https://{domain}.darkorbit.com/");
        string frontPage;
        try
        {
            _logger.LogDebug("Fetching DarkOrbit front page from {Url}", frontPageUrl);
            frontPage = await BotHttpClient.Create(frontPageUrl.ToString())
                .GetContentAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DarkOrbit front page");
            throw new LoginException("Failed to load DarkOrbit front page.", ex);
        }

        var hasCaptcha = LoginHtmlParser.HasCaptcha(frontPage);
        _logger.LogDebug("Front page loaded ({Length} chars), captchaRequired={CaptchaRequired}", frontPage.Length, hasCaptcha);

        IReadOnlyDictionary<string, string> captchaParams;
        try
        {
            captchaParams = await _captchaSolver
                .SolveAsync(frontPageUrl, frontPage, new CaptchaSolveContext { ManualToken = captchaToken }, cancellationToken)
                .ConfigureAwait(false);
            if (captchaParams.Count > 0)
                _logger.LogInformation("Captcha parameters resolved for login POST");
        }
        catch (CaptchaException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Captcha solver failed");
            throw new CaptchaException("Captcha solver failed.", ex);
        }

        var loginUrl = LoginHtmlParser.GetLoginUrl(frontPage);
        _logger.LogDebug("Posting credentials to {LoginUrl}", loginUrl);

        var cookieContainer = new CookieContainer();
        using var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", BotHttpClient.DefaultUserAgent);
        request.Content = new FormUrlEncodedContent(BuildLoginForm(loginData, captchaParams));

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(
                "Login POST finished: HTTP {StatusCode}, bodyLength={BodyLength}",
                (int)response.StatusCode,
                body.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login POST request failed");
        }

        var (sid, instanceHost) = LoginHtmlParser.ExtractDosidCookie(cookieContainer.GetAllCookies());
        loginData.SetSid(sid, instanceHost);

        _logger.LogInformation(
            "Credential login HTTP complete: instance={Instance}, sidSuffix={SidSuffix}",
            instanceHost,
            sid.Length >= 4 ? sid[^4..] : sid);
    }

    private void LogLoginSuccess(LoginData loginData, string method)
    {
        _logger.LogInformation(
            "Login ({Method}) succeeded: userId={UserId}, instance={Instance}, sidSuffix={SidSuffix}",
            method,
            loginData.UserId,
            loginData.InstanceHost,
            loginData.Sid is { Length: >= 4 } sid ? sid[^4..] : loginData.Sid);

        if (!string.IsNullOrWhiteSpace(loginData.Sid))
        {
            _logger.LogInformation(
                "KekkaPlayer test session: dosid={Dosid}, preloader={PreloaderUrl}",
                loginData.Sid,
                loginData.PreloaderUrl);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> BuildLoginForm(
        LoginData loginData,
        IReadOnlyDictionary<string, string> captchaParams)
    {
        yield return new KeyValuePair<string, string>("username", loginData.Username!);
        yield return new KeyValuePair<string, string>("password", loginData.Password!);

        foreach (var (key, value) in captchaParams)
            yield return new KeyValuePair<string, string>(key, value);
    }

    private static HttpClient CreateRedirectDisabledClient() => new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
}
