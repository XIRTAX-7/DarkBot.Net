using System.Net;

namespace DarkBot.Net.Infrastructure.Auth.Tests;

public class LoginHtmlParserTests
{
    [Fact]
    public void GetLoginUrl_parses_frontpage_form()
    {
        const string html = """
            "bgcdw_login_form" action="https://www.darkorbit.com/index.es?action=login&amp;login=1"
            """;

        var url = LoginHtmlParser.GetLoginUrl(html);

        Assert.Equal("https://www.darkorbit.com/index.es?action=login&login=1", url);
    }

    [Fact]
    public void ParseFlashEmbed_extracts_preloader_and_params()
    {
        const string line = """
            flashembed("container", {"src": "https://ru1.darkorbit.com/preloader.swf"}, {"userID":"12345","lang":"en"})
            """;

        var (preloaderUrl, paramsJson) = LoginHtmlParser.ParseFlashEmbed(line);

        Assert.Equal("https://ru1.darkorbit.com/preloader.swf", preloaderUrl);
        Assert.Contains("12345", paramsJson);
    }

    [Fact]
    public void ExtractDosidCookie_filters_server_domain()
    {
        var cookies = new CookieCollection
        {
            new Cookie("dosid", "abc123", "/", "www.darkorbit.com"),
            new Cookie("dosid", "serverSid", "/", "ru1.darkorbit.com")
        };

        var (sid, domain) = LoginHtmlParser.ExtractDosidCookie(cookies);

        Assert.Equal("serverSid", sid);
        Assert.Equal("ru1.darkorbit.com", domain);
    }

    [Fact]
    public void HasCaptcha_detects_marker()
    {
        Assert.True(LoginHtmlParser.HasCaptcha("""<div class="bgcdw_captcha"></div>"""));
        Assert.False(LoginHtmlParser.HasCaptcha("<div></div>"));
    }

    [Fact]
    public void GetCaptchaSiteKey_parses_attribute()
    {
        const string html = """<div data-sitekey="site-key-123"></div>""";

        Assert.Equal("site-key-123", LoginHtmlParser.GetCaptchaSiteKey(html));
    }
}
