using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Options;
using DarkBot.Net.Application.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Application.Bot;

/// <summary>Port of BotInstaller — address registry from Frida /status only (Frida-only).</summary>
public sealed class BotInstallerService : IHostedService, IDisposable
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameConnection _game;
    private readonly IGameInstallerProbe? _installerProbe;
    private readonly ILogger<BotInstallerService> _logger;
    private long _lastInternetRead;
    private long _invalidTimerDeadlineMs;
    private bool _invalidTimerArmed;
    private long _installedScreenManager;

    public BotInstallerService(
        BotAddressRegistry addresses,
        IGameConnection game,
        ILogger<BotInstallerService> logger,
        IGameInstallerProbe? installerProbe = null)
    {
        _addresses = addresses;
        _game = game;
        _installerProbe = installerProbe;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Tick()
    {
        if (_game.Mode == GameApiMode.UnityClient)
            _installerProbe?.RefreshStatus();

        if (_addresses.IsInvalid)
        {
            if (!_game.IsLaunched)
                return;

            CheckInvalid();
            if (TryInstall())
                return;

            _invalidTimerArmed = false;
        }
        else if (_game.IsValid)
        {
            ValidateInstalledAddresses();
        }
        else
        {
            _addresses.MarkInvalid();
        }
    }

    public int GetRecommendedDelayMs() => _addresses.IsInvalid ? 250 : 100;

    private void CheckInvalid()
    {
        var lastRead = _game.LastInternetReadTime();
        if (_lastInternetRead != lastRead)
        {
            _lastInternetRead = lastRead;
            ArmInvalidTimer(60_000);
        }
        else if (!_invalidTimerArmed)
        {
            ArmInvalidTimer(60_000);
        }

        if (_invalidTimerArmed && Environment.TickCount64 >= _invalidTimerDeadlineMs)
        {
            _invalidTimerArmed = false;
            _game.ClearCache(".*");
            _game.HandleRefresh(useFakeDailyLogin: true);
            _logger.LogWarning("Triggering refresh: stuck at loading screen for too long");
        }
    }

    private bool TryInstall()
    {
        if (!_game.IsValid || _installerProbe is null)
            return true;

        _installerProbe.RefreshStatus();
        if (!_installerProbe.TryGetInstallerAddresses(
                out var mainApplicationAddress,
                out var mainAddress,
                out var screenManagerAddress,
                out var connectionManagerAddress))
        {
            return true;
        }

        if (_installedScreenManager == screenManagerAddress && !_addresses.IsInvalid)
            return false;

        _addresses.SetMainApplicationAddress(mainApplicationAddress);
        _addresses.SetMainAddress(mainAddress);
        _addresses.SetScreenManagerAddress(screenManagerAddress);
        _installedScreenManager = screenManagerAddress;

        if (connectionManagerAddress != 0)
            _addresses.SetConnectionManagerAddress(connectionManagerAddress);

        _addresses.SetHeroInfoAddress(0);

        _logger.LogInformation(
            "BotInstaller connected (Frida): screenManager=0x{ScreenManager:X}, main=0x{Main:X}",
            screenManagerAddress,
            mainAddress);

        return false;
    }

    private void ValidateInstalledAddresses()
    {
        if (_game.Mode == GameApiMode.UnityClient && !_game.IsValid)
        {
            _logger.LogDebug("Frida no longer ready — invalidating addresses");
            _installedScreenManager = 0;
            _addresses.MarkInvalid();
        }
    }

    private void ArmInvalidTimer(long delayMs)
    {
        _invalidTimerArmed = true;
        _invalidTimerDeadlineMs = Environment.TickCount64 + delayMs;
    }

    public void Dispose() { }
}
