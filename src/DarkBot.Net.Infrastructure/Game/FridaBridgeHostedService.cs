using DarkBot.Net.Core.Options;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Subscribes to darkDev Frida bridge WS push (ws://127.0.0.1:port/ws).</summary>
public sealed class FridaBridgeHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FridaGameApi _frida;
    private readonly GameApiOptions _options;
    private readonly ILogger<FridaBridgeHostedService> _logger;

    public FridaBridgeHostedService(
        FridaGameApi frida,
        IOptions<GameApiOptions> options,
        ILogger<FridaBridgeHostedService> logger)
    {
        _frida = frida;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                var uri = new Uri($"ws://127.0.0.1:{_options.FridaApiPort}/ws");
                await socket.ConnectAsync(uri, stoppingToken).ConfigureAwait(false);
                _frida.NotifyBridgeConnected();
                _logger.LogInformation("Frida bridge WS connected on :{Port}/ws", _options.FridaApiPort);

                var buffer = new byte[64 * 1024];
                while (socket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                {
                    using var message = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, stoppingToken).ConfigureAwait(false);
                        message.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    var json = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                    await HandleMessageAsync(socket, json, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _frida.NotifyBridgeDisconnected();
                _logger.LogDebug(ex, "Frida bridge WS disconnected — retry in 3s");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleMessageAsync(ClientWebSocket socket, string json, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeProp))
            return;

        var type = typeProp.GetString();
        switch (type)
        {
            case "status":
            {
                var kind = root.TryGetProperty("kind", out var kindProp)
                    ? kindProp.GetString()
                    : "update";
                if (!root.TryGetProperty("data", out var data))
                    return;

                var status = data.Deserialize<FridaBridgeStatus>(JsonOptions);
                if (status is not null)
                    _frida.ApplyStatus(status, isSnapshot: kind == "snapshot");
                break;
            }
            case "ping":
                _frida.RecordBridgeActivity();
                await SendPongAsync(socket, root, cancellationToken).ConfigureAwait(false);
                break;
            case "event":
                _frida.RecordBridgeActivity();
                if (root.TryGetProperty("name", out var nameProp)
                    && nameProp.GetString() == "ready"
                    && root.TryGetProperty("data", out var eventData))
                {
                    var status = eventData.Deserialize<FridaBridgeStatus>(JsonOptions);
                    if (status is not null)
                        _frida.ApplyStatus(status, isSnapshot: true);
                }
                break;
        }
    }

    private static async Task SendPongAsync(
        ClientWebSocket socket,
        JsonElement pingRoot,
        CancellationToken cancellationToken)
    {
        long ts = pingRoot.TryGetProperty("ts", out var tsProp) && tsProp.TryGetInt64(out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var pong = $"{{\"type\":\"pong\",\"ts\":{ts}}}";
        var bytes = Encoding.UTF8.GetBytes(pong);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken)
            .ConfigureAwait(false);
    }
}
