using DarkBot.Net.Core.Options;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Auth;

public sealed class BackpageSidecarLocator
{
    private readonly LoginOptions _options;
    private readonly ILogger<BackpageSidecarLocator> _logger;

    public BackpageSidecarLocator(IOptions<LoginOptions> options, ILogger<BackpageSidecarLocator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string? ResolveExecutablePath()
    {
        var path = ResolvePath(_options.BackpageSidecarPath);
        if (!File.Exists(path))
        {
            _logger.LogDebug("DarkBackpage sidecar not found at {Path}", path);
            return null;
        }

        return path;
    }

    public bool IsVersionSupported()
    {
        var executable = ResolveExecutablePath();
        if (executable is null)
            return false;

        var versionFile = Path.Combine(Path.GetDirectoryName(executable) ?? string.Empty, ".version");
        if (!File.Exists(versionFile))
            return true;

        try
        {
            var current = Version.Parse(File.ReadAllText(versionFile).Trim());
            var minimum = Version.Parse(_options.BackpageSidecarMinVersion);
            return current >= minimum;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read DarkBackpage version file");
            return true;
        }
    }

    public Process StartProcess(params string[] args)
    {
        var executable = ResolveExecutablePath()
                         ?? throw new InvalidOperationException(
                             $"DarkBackpage sidecar not found at '{_options.BackpageSidecarPath}'. " +
                             "Install dark_backpage or provide a manual captcha token.");

        if (!IsVersionSupported())
        {
            throw new InvalidOperationException(
                $"DarkBackpage sidecar must be version {_options.BackpageSidecarMinVersion} or newer.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        return Process.Start(startInfo)
               ?? throw new InvalidOperationException("Failed to start DarkBackpage sidecar process.");
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
