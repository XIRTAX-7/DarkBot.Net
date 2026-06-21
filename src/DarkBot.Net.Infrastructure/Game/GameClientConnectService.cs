using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Infrastructure.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Waits for Pepper + Frida bridge after Darkorbit-client launch (Frida-only, no DarkMem).</summary>
public sealed class GameClientConnectService
{
    private readonly FridaGameApi _frida;
    private readonly ElectronControlClient _control;
    private readonly GameApiOptions _options;
    private readonly ILogger<GameClientConnectService> _logger;

    public GameClientConnectService(
        FridaGameApi frida,
        ElectronControlClient control,
        IOptions<GameApiOptions> options,
        ILogger<GameClientConnectService> logger)
    {
        _frida = frida;
        _control = control;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GameClientConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var pepperPid = await WaitForPepperPidAsync(cancellationToken).ConfigureAwait(false);
        if (pepperPid == 0)
        {
            return GameClientConnectResult.Fail(
                "Pepper Flash process not found. Open the game map in Darkorbit-client.");
        }

        _frida.AttachProcess(pepperPid);
        _logger.LogInformation("Game client Pepper pid={Pid} (Frida-only attach)", pepperPid);

        var fridaReady = await _frida.WaitForReadyAsync(cancellationToken).ConfigureAwait(false);
        if (!fridaReady)
        {
            return GameClientConnectResult.Fail(
                $"Frida game API not ready on :{_options.FridaApiPort}. Stay on the map and retry.");
        }

        return GameClientConnectResult.Ok(pepperPid);
    }

    private async Task<int> WaitForPepperPidAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.ClientConnectTimeoutSec);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var controlPid = await TryGetPepperPidFromControlAsync(cancellationToken).ConfigureAwait(false);
            if (controlPid > 0)
            {
                _logger.LogInformation("Detected Pepper pid={Pid} via control WS", controlPid);
                return controlPid;
            }

            await Task.Delay(_options.ConnectPollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private async Task<int> TryGetPepperPidFromControlAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _control.TryConnectAsync(cancellationToken).ConfigureAwait(false))
                return 0;

            return await _control.GetPepperPidAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Control WS getPid failed");
            return 0;
        }
    }
}
