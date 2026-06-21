namespace DarkBot.Net.Presentation.Tests.Helpers;

internal static class AsyncWaitHelpers
{
    public static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan pollInterval = default)
    {
        pollInterval = pollInterval == default ? TimeSpan.FromMilliseconds(25) : pollInterval;
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(pollInterval);
        }

        throw new TimeoutException("Condition was not met before timeout.");
    }
}
