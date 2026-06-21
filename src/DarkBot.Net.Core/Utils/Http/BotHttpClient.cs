using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DarkBot.Net.Core.Utils.Http;

/// <summary>Port of eu.darkbot.util.http.Http — builder-style HTTP client.</summary>
public sealed class BotHttpClient
{
    public static string DefaultUserAgent { get; set; } = "BigpointClient/1.6.7";

    private readonly string _baseUrl;
    private readonly HttpRequestMethod _method;
    private readonly bool _followRedirects;
    private readonly BodyHolder _body = new();
    private string _userAgent = DefaultUserAgent;
    private readonly List<Action> _suppliers = [];
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    private BotHttpClient(string baseUrl, HttpRequestMethod method, bool followRedirects)
    {
        _baseUrl = baseUrl;
        _method = method;
        _followRedirects = followRedirects;
    }

    public static BotHttpClient Create(string url) => new(url, HttpRequestMethod.Get, true);

    public static BotHttpClient Create(string url, HttpRequestMethod method) => new(url, method, true);

    public BotHttpClient AddSupplier(Action action)
    {
        _suppliers.Add(action);
        return this;
    }

    public BotHttpClient SetRawHeader(string key, string value)
    {
        _headers[key] = value;
        return this;
    }

    public BotHttpClient SetParam(object key, object value)
    {
        _body.SetParam(key, value);
        return this;
    }

    public BotHttpClient SetBody(byte[] body)
    {
        _body.SetBody(body);
        return this;
    }

    public BotHttpClient SetJsonBody(object json)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(json);
        return SetBody(bytes);
    }

    public BotHttpClient SetUserAgent(string userAgent)
    {
        _userAgent = userAgent;
        return this;
    }

    public Uri GetUri()
    {
        var url = _baseUrl;
        if (_method == HttpRequestMethod.Get && _body.HasParams && !url.Contains('?'))
            url += "?" + _body;

        return new Uri(url);
    }

    public async Task<string> GetContentAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> SendAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = _followRedirects
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        using var request = new HttpRequestMessage(
            _method == HttpRequestMethod.Post ? System.Net.Http.HttpMethod.Post : System.Net.Http.HttpMethod.Get,
            GetUri());

        request.Headers.UserAgent.ParseAdd(_userAgent);
        foreach (var (key, value) in _headers)
            request.Headers.TryAddWithoutValidation(key, value);

        if (_method == HttpRequestMethod.Post && _body.IsValid)
            request.Content = new ByteArrayContent(_body.GetBytes());

        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        foreach (var supplier in _suppliers)
            supplier();

        return response;
    }

    private sealed class BodyHolder
    {
        private readonly List<(string Key, string Value)> _params = [];
        private byte[]? _body;

        public bool HasParams => _params.Count > 0;
        public bool IsValid => _body is { Length: > 0 } || _params.Count > 0;

        public void SetParam(object key, object value) =>
            _params.Add((Uri.EscapeDataString(key.ToString()!), Uri.EscapeDataString(value.ToString()!)));

        public void SetBody(byte[] body) => _body = body;

        public byte[] GetBytes()
        {
            if (_body is { Length: > 0 })
                return _body;

            return Encoding.UTF8.GetBytes(ToString());
        }

        public override string ToString() =>
            string.Join('&', _params.Select(p => $"{p.Key}={p.Value}"));
    }
}
