using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game.Bridge;
using DarkBot.Net.Infrastructure.Game.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Application.Tests;

/// <summary>
/// Интеграционный тест FridaCLR + unity_bridge_agent.js.
/// Запуск: DarkOrbit.exe на карте + переменная DARKORBIT_INTEGRATION=1.
/// </summary>
public sealed class UnityFridaIntegrationTests
{
    [Fact]
    public async Task Attach_running_DarkOrbit_and_query_status()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DARKORBIT_INTEGRATION"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var options = Options.Create(new GameApiOptions
        {
            UnityProcessName = "DarkOrbit",
            FridaReadyTimeoutSec = 60
        });

        var finder = new UnityProcessFinder(options, NullLogger<UnityProcessFinder>.Instance);
        var pid = finder.FindRunningProcessId();
        Assert.True(pid > 0, "DarkOrbit.exe must be running on the map");

        using var session = new UnityFridaSession(options, NullLogger<UnityFridaSession>.Instance);
        await session.AttachAndLoadAgentAsync(pid);

        var statusJson = await session.GetStatusJsonAsync();
        Assert.False(string.IsNullOrWhiteSpace(statusJson));

        var status = UnityBridgeStatusMapper.ParseStatusJson(statusJson);
        Assert.NotNull(status);
        Assert.True(status!.Ready, $"Agent not ready: {statusJson}");

        var moveResult = await session.MoveToAsync(
            UnityBridgeStatusMapper.DefaultMapWidth / 2,
            UnityBridgeStatusMapper.DefaultMapHeight / 2);
        Assert.False(string.IsNullOrWhiteSpace(moveResult));
    }
}
