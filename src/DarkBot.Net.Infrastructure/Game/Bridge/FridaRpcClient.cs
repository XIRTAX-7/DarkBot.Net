using System.Collections.Concurrent;
using System.Text.Json;
using Frida;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Game.Bridge;

/// <summary>Вызов rpc.exports через протокол frida:rpc (FridaCLR не имеет script.exports).</summary>
public sealed class FridaRpcClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Script _script;
    private readonly ILogger _logger;
    private int _nextRequestId;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
    private bool _disposed;

    public FridaRpcClient(Script script, ILogger logger)
    {
        _script = script;
        _script.Message += OnScriptMessage;
        _logger = logger;
    }

    public void HandleIncomingMessage(string rawMessage)
    {
        if (!TryParseSendPayload(rawMessage, out var payload))
            return;

        if (payload.ValueKind != JsonValueKind.Array || payload.GetArrayLength() < 3)
            return;

        if (!string.Equals(payload[0].GetString(), "frida:rpc", StringComparison.Ordinal))
            return;

        var requestId = payload[1].GetInt32();
        if (!_pending.TryRemove(requestId, out var tcs))
            return;

        var status = payload[2].GetString();
        if (status == "ok")
        {
            tcs.TrySetResult(payload.GetArrayLength() > 3 ? payload[3] : default);
            return;
        }

        var error = payload.GetArrayLength() > 3
            ? payload[3].GetRawText()
            : "rpc error";
        tcs.TrySetException(new InvalidOperationException($"Frida RPC failed: {error}"));
    }

    public async Task<string?> CallStringAsync(
        string methodName,
        object?[]? args = null,
        CancellationToken cancellationToken = default)
    {
        var result = await CallRawAsync(methodName, args, cancellationToken).ConfigureAwait(false);
        return result.ValueKind switch
        {
            JsonValueKind.String => result.GetString(),
            JsonValueKind.Undefined => null,
            _ => result.GetRawText()
        };
    }

    public async Task<JsonElement> CallRawAsync(
        string methodName,
        object?[]? args = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var requestId = Interlocked.Increment(ref _nextRequestId);
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var request = JsonSerializer.Serialize(
            new object?[] { "frida:rpc", requestId, "call", methodName, args ?? [] },
            JsonOptions);

        _logger.LogDebug("Frida RPC call {Method} id={RequestId}", methodName, requestId);
        _script.Post(request);

        await using var registration = cancellationToken.Register(() =>
            tcs.TrySetCanceled(cancellationToken));

        try
        {
            return await tcs.Task.ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _pending.TryRemove(requestId, out _);
            throw;
        }
    }

    private void OnScriptMessage(object? sender, ScriptMessageEventArgs e) =>
        HandleIncomingMessage(e.Message);

    internal static bool TryParseSendPayload(string rawMessage, out JsonElement payload)
    {
        payload = default;
        try
        {
            using var doc = JsonDocument.Parse(rawMessage);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp)
                || typeProp.GetString() != "send"
                || !root.TryGetProperty("payload", out var payloadElement))
            {
                return false;
            }

            payload = payloadElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _script.Message -= OnScriptMessage;

        foreach (var (_, tcs) in _pending)
            tcs.TrySetCanceled();

        _pending.Clear();
    }
}
