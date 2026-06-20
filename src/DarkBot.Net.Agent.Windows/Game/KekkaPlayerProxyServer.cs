using System.Net;
using System.Net.Sockets;
using System.Text;
using DarkBot.Net.Agent.Windows.Bridge;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Port of KekkaPlayerProxyServer — blocks analytics requests via local proxy.</summary>
public sealed class KekkaPlayerProxyServer : IDisposable
{
    private readonly NativeKekkaBridge _kekka;
    private readonly ILogger<KekkaPlayerProxyServer> _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public KekkaPlayerProxyServer(NativeKekkaBridge kekka, ILogger<KekkaPlayerProxyServer>? logger = null)
    {
        _kekka = kekka;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<KekkaPlayerProxyServer>.Instance;
    }

    public int Port { get; private set; }

    public void Start() => StartWithoutNativeProxy();

    public void StartWithoutNativeProxy()
    {
        for (var port = 7777; port < 7877; port++)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                Port = port;
                _logger.LogInformation("Kekka proxy listening on port {Port}", port);
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                _logger.LogDebug("Proxy port {Port} in use", port);
            }
        }

        if (_listener is null)
            throw new InvalidOperationException("No free proxy port found.");

        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Proxy accept failed");
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient clientArg)
    {
        using var client = clientArg;
        client.NoDelay = true;

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true };

        var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(requestLine))
            return;

        while (!string.IsNullOrEmpty(await reader.ReadLineAsync().ConfigureAwait(false)))
        {
        }

        if (requestLine.Contains("deltadna.net", StringComparison.OrdinalIgnoreCase) ||
            requestLine.Contains("eventstream", StringComparison.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync("HTTP/1.1 404 Not Found").ConfigureAwait(false);
            await writer.WriteLineAsync("Connection: close").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
            return;
        }

        await writer.WriteLineAsync("HTTP/1.1 502 Bad Gateway").ConfigureAwait(false);
        await writer.WriteLineAsync("Connection: close").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _kekka.SetLocalProxy(0);
    }
}
