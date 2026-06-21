using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DarkBot.Net.Application.Tests.Helpers;

internal sealed class TestWebSocketServer : IAsyncDisposable
{
    private WebApplication? _app;
    private int _connectionCount;

    public int Port { get; private set; }

    public int ConnectionCount => Volatile.Read(ref _connectionCount);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();
        _app.UseWebSockets();
        _app.Map("/ws", AcceptConnectionAsync);
        _app.Map("/", AcceptConnectionAsync);

        await _app.StartAsync(cancellationToken).ConfigureAwait(false);

        var address = _app.Urls.First();
        Port = new Uri(address).Port;
    }

    private async Task AcceptConnectionAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        Interlocked.Increment(ref _connectionCount);

        try
        {
            var buffer = new byte[128];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        finally
        {
            Interlocked.Decrement(ref _connectionCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is null)
            return;

        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _app = null;
    }
}
