using System.Text.Json;

namespace DarkBot.Net.Login;

public sealed class LoginData
{
    public string? Username { get; private set; }
    public string? Password { get; private set; }
    public string? Sid { get; private set; }
    public string? InstanceHost { get; private set; }
    public int UserId { get; private set; }
    public string? PreloaderUrl { get; private set; }
    public IReadOnlyDictionary<string, string>? FlashParams { get; private set; }

    public Uri? InstanceUri =>
        string.IsNullOrWhiteSpace(InstanceHost) ? null : new Uri($"https://{InstanceHost.TrimEnd('/')}/");

    public bool IsNotInitialized => PreloaderUrl is null || FlashParams is null;

    public void Reset()
    {
        Username = null;
        Password = null;
        Sid = null;
        InstanceHost = null;
        UserId = 0;
        PreloaderUrl = null;
        FlashParams = null;
    }

    public void SetCredentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public void SetSid(string sid, string instanceHost)
    {
        Sid = sid;
        InstanceHost = NormalizeInstanceHost(instanceHost);
    }

    private static string NormalizeInstanceHost(string instanceHost)
    {
        var trimmed = instanceHost.Trim();
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            trimmed = new Uri(trimmed).Host;

        if (!trimmed.EndsWith(".darkorbit.com", StringComparison.OrdinalIgnoreCase))
            trimmed += ".darkorbit.com";

        return trimmed;
    }

    public void SetPreloader(string preloaderUrl, string paramsJson)
    {
        PreloaderUrl = preloaderUrl;
        FlashParams = JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson)
                      ?? new Dictionary<string, string>();

        if (FlashParams.TryGetValue("userID", out var userIdText) &&
            int.TryParse(userIdText, out var userId))
        {
            UserId = userId;
        }
    }
}
