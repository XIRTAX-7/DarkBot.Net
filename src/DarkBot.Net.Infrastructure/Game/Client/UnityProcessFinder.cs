using System.Diagnostics;
using DarkBot.Net.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game.Client;

/// <summary>Ищет запущенный Unity-клиент DarkOrbit.exe.</summary>
public sealed class UnityProcessFinder(IOptions<GameApiOptions> options, ILogger<UnityProcessFinder> logger)
{
    private readonly GameApiOptions _options = options.Value;

    public int FindRunningProcessId()
    {
        var processName = NormalizeProcessName(_options.UnityProcessName);
        var processes = Process.GetProcessesByName(processName);
        try
        {
            if (processes.Length == 0)
            {
                logger.LogDebug("Unity process {ProcessName} not found", processName);
                return 0;
            }

            var selected = processes
                .OrderByDescending(static p => p.StartTime)
                .First();

            logger.LogInformation(
                "Found Unity process {ProcessName} pid={Pid} ({Count} instance(s))",
                processName,
                selected.Id,
                processes.Length);

            return selected.Id;
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    private static string NormalizeProcessName(string? configured)
    {
        var name = string.IsNullOrWhiteSpace(configured) ? "DarkOrbit" : configured.Trim();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? name[..^4]
            : name;
    }
}
