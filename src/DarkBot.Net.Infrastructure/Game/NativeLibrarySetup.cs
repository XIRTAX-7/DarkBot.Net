using DarkBot.Net.Core.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Optional Flash OCX download — Frida-only runtime (no DarkMem / JNI).</summary>
public sealed class NativeLibrarySetup
{
    private const string ManifestUrl =
        "https://gist.github.com/Pablete1234/2e43458bb3b644e16d146969069b1548/raw/libs.json";

    private readonly GameApiOptions _options;
    private readonly ILogger<NativeLibrarySetup> _logger;
    private readonly HttpClient _http;

    public NativeLibrarySetup(IOptions<GameApiOptions> options, ILogger<NativeLibrarySetup> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public Task EnsureLibrariesAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            return Task.CompletedTask;

        return EnsureFlashOcxAsync(cancellationToken);
    }

    public string ResolveFlashOcxPath()
    {
        var ocxName = OperatingSystem.IsWindowsVersionAtLeast(6, 2)
            ? "DarkFlash.ocx"
            : "DarkFlash-W7.ocx";

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DarkBot",
            "lib",
            ocxName);

        if (File.Exists(appDataPath))
            return appDataPath;

        var libDir = ResolveLibDir();
        var libPath = Path.Combine(libDir, ocxName);
        return File.Exists(libPath) ? libPath : appDataPath;
    }

    private async Task EnsureFlashOcxAsync(CancellationToken cancellationToken)
    {
        var flashDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DarkBot",
            "lib");
        Directory.CreateDirectory(flashDir);

        try
        {
            var manifest = await _http.GetFromJsonAsync<Dictionary<string, LibEntry>>(
                ManifestUrl,
                cancellationToken).ConfigureAwait(false);

            if (manifest is null)
                return;

            foreach (var entry in manifest.Values.Where(static e =>
                         e.Auto && e.Path.Contains("DarkFlash", StringComparison.OrdinalIgnoreCase)))
            {
                var target = Path.Combine(flashDir, Path.GetFileName(entry.Path));
                await EnsureLibraryAsync(entry, target, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Flash OCX manifest download skipped");
        }
    }

    private static string ResolveLibDir()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DARKBOT_LIB_PATH")))
            return Path.GetFullPath(Environment.GetEnvironmentVariable("DARKBOT_LIB_PATH")!);

        var fromBase = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "lib"));
        return Directory.Exists(fromBase) ? fromBase : fromBase;
    }

    private async Task EnsureLibraryAsync(LibEntry entry, string targetPath, CancellationToken cancellationToken)
    {
        if (File.Exists(targetPath) && MatchesSha256(targetPath, entry))
            return;

        if (string.IsNullOrWhiteSpace(entry.Download))
            return;

        _logger.LogInformation("Downloading {Library} to {Path}", entry.Path, targetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await using var stream = await _http.GetStreamAsync(entry.Download, cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(targetPath);
        await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
    }

    private static bool MatchesSha256(string path, LibEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Sha256))
            return true;

        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        if (hash.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
            return true;

        return entry.AltSha256?.Any(alt => alt.Equals(hash, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private sealed class LibEntry
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string? Sha256 { get; set; }

        [JsonPropertyName("altSha256")]
        public List<string>? AltSha256 { get; set; }

        [JsonPropertyName("download")]
        public string? Download { get; set; }

        [JsonPropertyName("auto")]
        public bool Auto { get; set; }
    }
}
