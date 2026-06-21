namespace DarkBot.Net.Infrastructure.Auth;

public sealed class CaptchaSolveContext
{
    public string? ManualToken { get; init; }
}

public interface ICaptchaSolver
{
    Task<IReadOnlyDictionary<string, string>> SolveAsync(
        Uri pageUrl,
        string html,
        CaptchaSolveContext? context,
        CancellationToken cancellationToken = default);
}
