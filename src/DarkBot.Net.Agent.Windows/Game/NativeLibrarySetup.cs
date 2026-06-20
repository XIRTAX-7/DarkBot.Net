using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Port of LibSetup/LibUtils — ensure native libraries and Flash OCX are present.</summary>
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

    public void PrepareRuntimePath()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var serverBin = Path.Combine(javaHome, "bin", "server");
            if (Directory.Exists(serverBin))
                PrependPath(serverBin);
        }

        PrependPath(NativeBridgePaths.ResolveLibDir(_options.LibPath));
    }

    public async Task EnsureLibrariesAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DarkBot game client is only supported on Windows.");

        PrepareRuntimePath();

        var libDir = NativeBridgePaths.ResolveLibDir(_options.LibPath);
        Directory.CreateDirectory(libDir);

        NativeBridgePaths.EnsureDarkBotJarInLib(libDir, _options.DarkBotJarPath, _logger);

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

            if (manifest is not null)
            {
                foreach (var entry in manifest.Values.Where(static e => e.Auto))
                    await EnsureLibraryAsync(entry, ResolveTargetPath(entry, libDir, flashDir), cancellationToken)
                        .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download libs manifest; using existing local libraries");
        }

        EnsureRequiredArtifacts(libDir);
    }

    /// <summary>Resolves DarkFlash.ocx — AppData first (Java default), then ./lib fallback.</summary>
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

        var libDir = NativeBridgePaths.ResolveLibDir(_options.LibPath);
        var libPath = Path.Combine(libDir, ocxName);
        if (File.Exists(libPath))
            return libPath;

        return appDataPath;
    }

    public void EnsureFlashOcxPresent()
    {
        var flashPath = ResolveFlashOcxPath();
        if (File.Exists(flashPath))
        {
            TryRegisterFlashOcx(flashPath);
            _logger.LogDebug("DarkFlash OCX resolved: {Path}", flashPath);
            return;
        }

        throw new FileNotFoundException(
            $"DarkFlash OCX not found. Expected at {flashPath} or in ./lib. " +
            "Run Java DarkBot once or copy DarkFlash.ocx to %APPDATA%\\DarkBot\\lib\\.",
            flashPath);
    }

    private void TryRegisterFlashOcx(string flashPath)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "regsvr32",
                Arguments = $"/s \"{flashPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            process?.WaitForExit(10_000);
            if (process is { ExitCode: 0 })
                _logger.LogDebug("Registered DarkFlash OCX via regsvr32");
            else
                _logger.LogDebug("regsvr32 for DarkFlash OCX finished with exit code {ExitCode}", process?.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not register DarkFlash OCX via regsvr32");
        }
    }

    private static string ResolveTargetPath(LibEntry entry, string libDir, string flashDir)
    {
        if (entry.Path.Contains("DarkFlash", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(flashDir, Path.GetFileName(entry.Path));

        if (Path.IsPathRooted(entry.Path))
            return entry.Path;

        return Path.Combine(libDir, Path.GetFileName(entry.Path));
    }

    private async Task EnsureLibraryAsync(LibEntry entry, string targetPath, CancellationToken cancellationToken)
    {
        if (File.Exists(targetPath) && MatchesSha256(targetPath, entry))
            return;

        if (string.IsNullOrWhiteSpace(entry.Download))
            return;

        _logger.LogInformation("Downloading native library {Library} to {Path}", entry.Path, targetPath);
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

    private void EnsureRequiredArtifacts(string libDir)
    {
        var bridge = Path.Combine(libDir, "DarkBotBridge.dll");
        var darkMem = Path.Combine(libDir, "DarkMemAPI.dll");
        var kekka = Path.Combine(libDir, "KekkaPlayer.dll");
        var flash = ResolveFlashOcxPath();
        var classesDir = NativeBridgePaths.ResolveClassesDir(_options.ClassesPath);

        if (!File.Exists(bridge))
            _logger.LogWarning("Missing {File} in {LibDir}", Path.GetFileName(bridge), libDir);

        if (!File.Exists(darkMem))
            _logger.LogWarning("Missing {File} in {LibDir}", Path.GetFileName(darkMem), libDir);

        if (!File.Exists(kekka))
            _logger.LogWarning("Missing {File} in {LibDir}", Path.GetFileName(kekka), libDir);

        if (!File.Exists(flash))
            _logger.LogWarning("Missing Flash OCX at {Path}", flash);

        if (!Directory.Exists(classesDir))
            _logger.LogWarning("Bridge Java classes directory not found: {ClassesDir}", classesDir);
    }

    private static void PrependPath(string directory)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (path.Contains(directory, StringComparison.OrdinalIgnoreCase))
            return;

        Environment.SetEnvironmentVariable("PATH", $"{directory};{path}");
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
