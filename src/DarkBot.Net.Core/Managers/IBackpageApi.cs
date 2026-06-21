using DarkBot.Net.Core.Utils.Http;

namespace DarkBot.Net.Core.Managers;

public interface IBackpageApi : IApi.ISingleton
{
    bool IsInstanceValid();
    string SidStatus { get; }
    string? Sid { get; }
    int UserId { get; }
    Uri? InstanceUri { get; }
    DateTimeOffset LastRequestTime { get; }
    void UpdateLastRequestTime();

    BotHttpClient Http(string path, HttpRequestMethod method)
    {
        if (!IsInstanceValid())
            throw new InvalidOperationException("Can't connect when instance is invalid");

        return BotHttpClient.Create(InstanceUri + path, method)
            .SetRawHeader("Cookie", "dosid=" + Sid)
            .AddSupplier(UpdateLastRequestTime);
    }

    BotHttpClient Http(string path, HttpRequestMethod method, int minWaitMs)
    {
        Thread.Sleep(Math.Max(0, (int)(LastRequestTime.ToUnixTimeMilliseconds() + minWaitMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
        return Http(path, method);
    }

    BotHttpClient GetHttp(string path) => Http(path, HttpRequestMethod.Get);
    BotHttpClient GetHttp(string path, int minWaitMs) => Http(path, HttpRequestMethod.Get, minWaitMs);
    BotHttpClient PostHttp(string path) => Http(path, HttpRequestMethod.Post);
    BotHttpClient PostHttp(string path, int minWaitMs) => Http(path, HttpRequestMethod.Post, minWaitMs);

    string? FindReloadToken(string body);
    void SetSession(string sid, int userId, Uri instanceUri);
}
