namespace DarkBot.Net.Api.Managers;

/// <summary>Session fields mirrored from game memory (StatsManager) for backpage sync.</summary>
public interface ISessionMetadataProvider
{
    string? Sid { get; }
    int UserId { get; }
    string? InstanceUrl { get; }
    void UpdateSession(string sid, int userId, string instanceUrl);
}
