using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using DarkBot.Net.Presentation.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Presentation.Services.Game;

/// <summary>Starts verifier.jar sidecar or dev HTTP stub (POST /verify).</summary>
public sealed class VerifierSidecarHostedService : BackgroundService
{
    private readonly DarkBotUiOptions _options;
    private readonly ILogger<VerifierSidecarHostedService> _logger;
    private Process? _jarProcess;
    private HttpListener? _listener;

    public VerifierSidecarHostedService(IOptions<DarkBotUiOptions> options, ILogger<VerifierSidecarHostedService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsJarRunning => _jarProcess is { HasExited: false };
    public bool IsListening { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jarPath = ResolveVerifierJarPath();
        if (File.Exists(jarPath))
        {
            TryStartJar(jarPath);
        }
        else
        {
            _logger.LogWarning("Verifier JAR not found at {Path}", jarPath);
        }

        if (_options.VerifierDevBypass || !IsJarRunning)
            await RunDevStubAsync(stoppingToken).ConfigureAwait(false);
        else
            await WaitForProcessExitAsync(stoppingToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Stop();
        if (_jarProcess is { HasExited: false })
        {
            try
            {
                _jarProcess.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to stop verifier process");
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private void TryStartJar(string jarPath)
    {
        try
        {
            _jarProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{jarPath}\"",
                WorkingDirectory = Path.GetDirectoryName(jarPath) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (_jarProcess is null)
            {
                _logger.LogWarning("Failed to start verifier.jar process");
                return;
            }

            _logger.LogInformation("Started verifier.jar (pid {Pid})", _jarProcess.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not launch verifier.jar — dev stub will be used if enabled");
        }
    }

    private async Task RunDevStubAsync(CancellationToken stoppingToken)
    {
        if (!_options.VerifierDevBypass)
            return;

        _listener = new HttpListener();
        var prefix = $"http://127.0.0.1:{_options.VerifierPort}/";
        _listener.Prefixes.Add(prefix);

        try
        {
            _listener.Start();
            IsListening = true;
            _logger.LogInformation("Verifier dev stub listening on {Prefix}", prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start verifier dev stub on port {Port}", _options.VerifierPort);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_listener is null || !IsListening)
                break;

            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(stoppingToken).ConfigureAwait(false);
                await HandleRequestAsync(context, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException ex)
            {
                _logger.LogDebug(ex, "Verifier dev stub listener stopped");
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Verifier stub request failed");
                context?.Response.StatusCode = 500;
                context?.Response.Close();
            }
        }

        IsListening = false;
    }

    private static async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) ||
            !request.Url!.AbsolutePath.TrimEnd('/').Equals("/verify", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var ok = !string.IsNullOrWhiteSpace(body);

        response.StatusCode = ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Forbidden;
        response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new { ok, mode = "dev-stub" });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        response.Close();
    }

    private async Task WaitForProcessExitAsync(CancellationToken stoppingToken)
    {
        if (_jarProcess is null)
            return;

        try
        {
            await _jarProcess.WaitForExitAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogWarning("Verifier process exited with code {Code}", _jarProcess.ExitCode);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private string ResolveVerifierJarPath()
    {
        var configured = ResolvePath(_options.VerifierPath);
        if (File.Exists(configured))
            return configured;

        var libFallback = ResolvePath(Path.Combine(_options.LibPath, "verifier.jar"));
        return File.Exists(libFallback) ? libFallback : configured;
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
