using System.Text.RegularExpressions;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Api.Utils;
using DarkBot.Net.Api.Utils.Http;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Backpage;

/// <summary>Port of BackpageManager — HTTP session + SID validation.</summary>
public sealed partial class BackpageService : IBackpageApi
{
    private static readonly string[] SidCheckActions = ["internalDock", "internalDock&tpl=internalDockAmmo", "internalSkylab"];
    private static readonly TimeSpan ShopCooldown = TimeSpan.FromMinutes(20);

    private readonly ISessionMetadataProvider? _session;
    private readonly ILogger<BackpageService> _logger;
    private readonly BotTimer _shopTimer = BotTimer.Create((long)ShopCooldown.TotalMilliseconds);

    private string? _sid;
    private string? _instance;
    private Uri? _instanceUri;
    private int _userId;
    private BackpageSidStatus _status = BackpageSidStatus.Unknown;
    private DateTimeOffset _sidNextUpdate = DateTimeOffset.UtcNow;

    public BackpageService(IStatsApi stats, ILogger<BackpageService> logger)
    {
        _session = stats as ISessionMetadataProvider;
        _logger = logger;
    }

    public bool IsInstanceValid() =>
        !string.IsNullOrEmpty(_sid) && _userId != 0 && _instanceUri is not null;

    public string SidStatus => _status.ToStatusString();
    public string? Sid => _sid;
    public int UserId => _userId;
    public Uri? InstanceUri => _instanceUri;
    public DateTimeOffset LastRequestTime { get; private set; } = DateTimeOffset.UtcNow;

    public BackpageSidStatus Status => _status;

    public void UpdateLastRequestTime() => LastRequestTime = DateTimeOffset.UtcNow;

    public string? FindReloadToken(string body)
    {
        var match = ReloadTokenRegex().Match(body);
        return match.Success ? match.Groups[1].Value : null;
    }

    public void SetSession(string sid, int userId, Uri instanceUri)
    {
        _sid = sid;
        _userId = userId;
        _instance = instanceUri.ToString();
        _instanceUri = instanceUri;
        _status = BackpageSidStatus.Valid;
        _session?.UpdateSession(sid, userId, _instance);
        _logger.LogInformation(
            "Backpage session set: userId={UserId}, instance={Instance}, sidSuffix={SidSuffix}",
            userId,
            instanceUri.Host,
            sid.Length >= 4 ? sid[^4..] : sid);
    }

    internal void SyncFromStats()
    {
        if (_session is null)
            return;

        if (!string.IsNullOrEmpty(_session.Sid))
        {
            _sid = _session.Sid;
            _userId = _session.UserId;
        }

        if (!string.IsNullOrEmpty(_session.InstanceUrl) &&
            !string.Equals(_instance, _session.InstanceUrl, StringComparison.Ordinal))
        {
            _instance = _session.InstanceUrl;
            _instanceUri = TryParseUri(_instance);
        }
    }

    public bool CheckSidValid()
    {
        SyncFromStats();

        if (IsInvalid())
        {
            _status = BackpageSidStatus.NoSid;
            return false;
        }

        if (_status == BackpageSidStatus.NoSid)
            _status = BackpageSidStatus.Unknown;

        if (DateTimeOffset.UtcNow < _sidNextUpdate)
            return _status == BackpageSidStatus.Valid;

        var previousStatus = _status;

        try
        {
            if (_shopTimer.IsInactive)
            {
                var action = SidCheckActions[Random.Shared.Next(SidCheckActions.Length)];
                var responseCode = GetResponseCodeAsync(action).GetAwaiter().GetResult();
                _status = BackpageSidStatusExtensions.FromResponseCode(responseCode);
                _logger.LogDebug(
                    "Backpage SID check via {Action}: HTTP {ResponseCode} -> {Status}",
                    action,
                    responseCode,
                    _status);
            }
            else
            {
                var responseCode = GetResponseCodeAsync("internalDock&tpl=internalDockAmmo").GetAwaiter().GetResult();
                _status = BackpageSidStatusExtensions.FromResponseCode(responseCode);
                _logger.LogDebug(
                    "Backpage SID check (shop cooldown): HTTP {ResponseCode} -> {Status}",
                    responseCode,
                    _status);
            }
        }
        catch (Exception ex)
        {
            _status = BackpageSidStatus.Error;
            _logger.LogWarning(ex, "Backpage SID check HTTP request failed");
        }

        if (_status != previousStatus)
        {
            _logger.LogInformation(
                "Backpage SID status changed: {Previous} -> {Current}",
                previousStatus,
                _status);
        }

        var waitMinutes = _status == BackpageSidStatus.Error ? 5 : 8;
        _sidNextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(waitMinutes + Random.Shared.NextDouble() * waitMinutes);
        return _status == BackpageSidStatus.Valid;
    }

    private bool IsInvalid() =>
        string.IsNullOrEmpty(_sid) || string.IsNullOrEmpty(_instance) || _userId == 0;

    internal async Task<int> GetResponseCodeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!IsInstanceValid())
            throw new InvalidOperationException("Can't connect when instance is invalid");

        if (path.Contains("shop.php", StringComparison.OrdinalIgnoreCase))
            _shopTimer.Activate();

        using var client = CreateHttpClient();
        var requestPath = BuildInternalActionPath(path);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_instanceUri!, requestPath));
        request.Headers.TryAddWithoutValidation("User-Agent", BotHttpClient.DefaultUserAgent);
        request.Headers.TryAddWithoutValidation("Cookie", "dosid=" + _sid);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        LastRequestTime = DateTimeOffset.UtcNow;
        return (int)response.StatusCode;
    }

    private static string BuildInternalActionPath(string action) =>
        action.StartsWith("indexInternal.es", StringComparison.OrdinalIgnoreCase)
            ? action
            : $"indexInternal.es?action={action}";

    private static HttpClient CreateHttpClient() => new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static Uri? TryParseUri(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        return Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ? parsed : null;
    }

    [GeneratedRegex("reloadToken=([^\"]+)")]
    private static partial Regex ReloadTokenRegex();
}
