using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Utils.Http;
using DarkBot.Net.Infrastructure.Auth;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Загружает LoginNodeData JSON для hook-bootstrap Unity x86.
/// Повторяет HTTP-шаг игры: GetPost(indexInternal.es?action=internalWebGL) после dosid.
/// </summary>
public sealed class UnityWebGlLoginResolver(ILogger<UnityWebGlLoginResolver> logger)
{
    private const int MinLoginJsonLength = 128;

    public string BuildLaunchUrl(Uri instanceUri) =>
        UnityGameLaunchUrls.BuildWebGlLoginUrl(instanceUri);

    public async Task<UnityWebGlSession?> FetchSessionAsync(
        string instanceHost,
        string sid,
        CancellationToken cancellationToken = default)
    {
        var baseUri = new Uri($"https://{instanceHost}/");
        var trimmedSid = sid.Trim();
        var attempts = new (string Label, HttpMethod Method, string Url)[]
        {
            ("POST internalWebGL", HttpMethod.Post, UnityGameLaunchUrls.BuildWebGlLoginUrl(baseUri)),
            ("GET internalWebGL", HttpMethod.Get, UnityGameLaunchUrls.BuildWebGlLoginUrl(baseUri)),
            ("GET internalMapRevolution", HttpMethod.Get, UnityGameLaunchUrls.BuildMapRevolutionUrl(baseUri)),
        };

        foreach (var (label, method, url) in attempts)
        {
            var body = await TryFetchAsync(label, method, url, trimmedSid, cancellationToken)
                .ConfigureAwait(false);
            if (body is null)
                continue;

            var json = TryExtractLoginJson(body);
            if (json is null)
            {
                logger.LogWarning(
                    "Session fetch {Label} returned {Length} bytes — no LoginNodeData JSON (preview={Preview})",
                    label,
                    body.Length,
                    PreviewBody(body));
                continue;
            }

            logger.LogInformation(
                "Session JSON resolved via {Label} for {Host} ({Length} bytes)",
                label,
                instanceHost,
                json.Length);
            return new UnityWebGlSession(instanceHost, trimmedSid, json);
        }

        logger.LogWarning("All session fetch strategies failed for {Host}", instanceHost);
        return null;
    }

    public static UnityWebGlSession? TryCreateFromFlashParams(
        string instanceHost,
        string sid,
        IReadOnlyDictionary<string, string> flashParams)
    {
        if (flashParams.Count == 0)
            return null;

        var json = JsonSerializer.Serialize(flashParams);
        return LooksLikeLoginNodeJson(json)
            ? new UnityWebGlSession(instanceHost, sid.Trim(), json)
            : null;
    }

    public async Task<bool> ValidateSessionAsync(
        string instanceHost,
        string sid,
        CancellationToken cancellationToken = default)
    {
        var session = await FetchSessionAsync(instanceHost, sid, cancellationToken).ConfigureAwait(false);
        return session is not null;
    }

    internal static string? TryExtractLoginJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            var fromWrapper = TryUnwrapLoginDataNew(trimmed);
            if (fromWrapper is not null)
                return fromWrapper;

            return LooksLikeLoginNodeJson(trimmed) ? trimmed : null;
        }

        var flashLine = LoginHtmlParser.FindFlashEmbedLine(body);
        if (flashLine is null)
            return null;

        try
        {
            var (_, paramsJson) = LoginHtmlParser.ParseFlashEmbed(flashLine);
            return LooksLikeLoginNodeJson(paramsJson) ? paramsJson : null;
        }
        catch (LoginException)
        {
            return null;
        }
    }

    internal static string? TryUnwrapLoginDataNew(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            if (!doc.RootElement.TryGetProperty("data", out var dataElement))
                return null;

            if (dataElement.ValueKind != JsonValueKind.String)
                return null;

            var inner = dataElement.GetString();
            if (string.IsNullOrWhiteSpace(inner))
                return null;

            return LooksLikeLoginNodeJson(inner) ? inner : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static bool LooksLikeLoginNodeJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length < MinLoginJsonLength)
            return false;

        if (!body.TrimStart().StartsWith('{'))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            return doc.RootElement.TryGetProperty("userID", out _)
                || doc.RootElement.TryGetProperty("userId", out _)
                || doc.RootElement.TryGetProperty("mapID", out _)
                || doc.RootElement.TryGetProperty("mapId", out _)
                || doc.RootElement.TryGetProperty("sessionID", out _)
                || doc.RootElement.TryGetProperty("sessionId", out _)
                || doc.RootElement.TryGetProperty("itemXmlHash", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string PreviewBody(string body)
    {
        var flat = body.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return flat.Length <= 120 ? flat : flat[..120] + "...";
    }

    private async Task<string?> TryFetchAsync(
        string label,
        HttpMethod method,
        string url,
        string sid,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            using var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Cookie", "dosid=" + sid);
            request.Headers.TryAddWithoutValidation("User-Agent", BotHttpClient.DefaultUserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            if (method == HttpMethod.Post)
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded");

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "Session fetch {Label}: HTTP {StatusCode}, length={Length}, preview={Preview}",
                label,
                (int)response.StatusCode,
                body.Length,
                PreviewBody(body));

            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
                return null;

            return body;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Session fetch {Label} error", label);
            return null;
        }
    }
}
