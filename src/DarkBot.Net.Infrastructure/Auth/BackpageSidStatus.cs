namespace DarkBot.Net.Infrastructure.Auth;

public enum BackpageSidStatus
{
    Unknown,
    NoSid,
    Valid,
    Error,
    Invalid
}

public static class BackpageSidStatusExtensions
{
    public static BackpageSidStatus FromResponseCode(int responseCode) => responseCode switch
    {
        200 => BackpageSidStatus.Valid,
        302 => BackpageSidStatus.Invalid,
        _ => BackpageSidStatus.Error
    };

    public static string ToStatusString(this BackpageSidStatus status) => status switch
    {
        BackpageSidStatus.Unknown => "?",
        BackpageSidStatus.NoSid => "--",
        BackpageSidStatus.Valid => "OK",
        BackpageSidStatus.Error => "ERR",
        BackpageSidStatus.Invalid => "KO",
        _ => "?"
    };
}
