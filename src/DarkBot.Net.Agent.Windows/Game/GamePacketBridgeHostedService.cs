using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>Connects to packet_dumper WebSocket and forwards JSON packets to <see cref="GamePacketReader"/>.</summary>
public sealed class GamePacketBridgeHostedService : BackgroundService
{
    private readonly GamePacketReader _reader;
    private readonly GameApiOptions _options;
    private readonly ILogger<GamePacketBridgeHostedService> _logger;

    public GamePacketBridgeHostedService(
        GamePacketReader reader,
        IOptions<GameApiOptions> options,
        ILogger<GamePacketBridgeHostedService> logger)
    {
        _reader = reader;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnablePacketBridge)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{_options.PacketPort}"), stoppingToken)
                    .ConfigureAwait(false);
                _logger.LogInformation("Packet bridge connected on :{Port}", _options.PacketPort);

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
                    _reader.HandleMessage(json);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Packet bridge disconnected — retry in 3s");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
