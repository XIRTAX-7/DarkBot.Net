using System.Buffers.Binary;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Agent.Windows.Game;

/// <summary>WebSocket client for Darkorbit-client control plane (reload, pid, window ops).</summary>
public sealed class ElectronControlClient : IDisposable
{
    private readonly GameApiOptions _options;
    private readonly ILogger<ElectronControlClient> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private ClientWebSocket? _socket;

    public ElectronControlClient(IOptions<GameApiOptions> options, ILogger<ElectronControlClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return true;

        _socket?.Dispose();
        _socket = new ClientWebSocket();

        try
        {
            await _socket.ConnectAsync(ControlUri(), cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Connected to Darkorbit-client control WS :{Port}", _options.ControlPort);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Control WS connect failed on :{Port}", _options.ControlPort);
            _socket.Dispose();
            _socket = null;
            return false;
        }
    }

    public async Task<int> GetPepperPidAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendReceiveAsync(ElectronControlOpcodes.GetPepperPid, null, 6, cancellationToken)
            .ConfigureAwait(false);
        if (response.Length < 6)
            return 0;

        return BinaryPrimitives.ReadInt32BigEndian(response.AsSpan(2, 4));
    }

    public async Task<(int Major, int Minor, int Patch)?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendReceiveAsync(ElectronControlOpcodes.GetVersion, null, 8, cancellationToken)
            .ConfigureAwait(false);
        if (response.Length < 8)
            return null;

        return (
            BinaryPrimitives.ReadInt16BigEndian(response.AsSpan(2, 2)),
            BinaryPrimitives.ReadInt16BigEndian(response.AsSpan(4, 2)),
            BinaryPrimitives.ReadInt16BigEndian(response.AsSpan(6, 2)));
    }

    public async Task<bool> IsPageValidAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendReceiveAsync(ElectronControlOpcodes.IsValid, null, 4, cancellationToken)
            .ConfigureAwait(false);
        if (response.Length < 4)
            return false;

        return BinaryPrimitives.ReadInt16BigEndian(response.AsSpan(2, 2)) == 1;
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await SendOnlyAsync(ElectronControlOpcodes.Reload, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Requested Darkorbit-client page reload via control WS");
    }

    public async Task SetSizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), height);
        await SendOnlyAsync(ElectronControlOpcodes.SetSize, cancellationToken, payload).ConfigureAwait(false);
    }

    public async Task SetVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        var payload = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(payload, (short)(visible ? 1 : 0));
        await SendOnlyAsync(ElectronControlOpcodes.SetVisible, cancellationToken, payload).ConfigureAwait(false);
    }

    public async Task SetMinimizedAsync(bool minimized, CancellationToken cancellationToken = default)
    {
        var payload = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(payload, (short)(minimized ? 1 : 0));
        await SendOnlyAsync(ElectronControlOpcodes.SetMinimized, cancellationToken, payload).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _sendLock.Dispose();
        _socket?.Dispose();
    }

    private Uri ControlUri() => new($"ws://127.0.0.1:{_options.ControlPort}");

    private async Task SendOnlyAsync(short opcode, CancellationToken cancellationToken, byte[]? payload = null)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await TryConnectAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException($"Darkorbit-client control WS unavailable on :{_options.ControlPort}");

            var request = BuildRequest(opcode, payload);
            await _socket!.SendAsync(request, WebSocketMessageType.Binary, true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<byte[]> SendReceiveAsync(
        short opcode,
        byte[]? payload,
        int expectedLength,
        CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await TryConnectAsync(cancellationToken).ConfigureAwait(false))
                return [];

            var request = BuildRequest(opcode, payload);
            await _socket!.SendAsync(request, WebSocketMessageType.Binary, true, cancellationToken)
                .ConfigureAwait(false);

            var buffer = new byte[Math.Max(expectedLength, 256)];
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            return ms.ToArray();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static byte[] BuildRequest(short opcode, byte[]? payload)
    {
        var request = new byte[2 + (payload?.Length ?? 0)];
        BinaryPrimitives.WriteInt16BigEndian(request.AsSpan(0, 2), opcode);
        payload?.CopyTo(request, 2);
        return request;
    }
}
