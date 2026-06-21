using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Agent.Windows.Memory;
using DarkBot.Net.Core.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Core.Bot;

/// <summary>Port of BotInstaller — memory scan and address registry population.</summary>
public sealed class BotInstallerService : IHostedService, IDisposable
{
    private readonly BotAddressRegistry _addresses;
    private readonly IGameConnection _game;
    private readonly ExtraMemoryReader _extraMemory;
    private readonly ILogger<BotInstallerService> _logger;
    private long _lastInternetRead;
    private long _invalidTimerDeadlineMs;
    private bool _invalidTimerArmed;
    private long _installedScreenManager;

    public BotInstallerService(
        BotAddressRegistry addresses,
        IGameConnection game,
        ExtraMemoryReader extraMemory,
        ILogger<BotInstallerService> logger)
    {
        _addresses = addresses;
        _game = game;
        _extraMemory = extraMemory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Tick()
    {
        if (_game.Mode == GameApiMode.FridaClient && _game is FridaGameApi fridaApi)
            fridaApi.RefreshStatus();

        if (_addresses.IsInvalid)
        {
            // Skip installer until the game client has been launched — mirrors Java's
            // BACKGROUND_ONLY / pre-createWindow guard.
            if (!_game.IsLaunched)
                return;

            CheckInvalid();
            if (TryInstall())
                return;

            // Install succeeded — disarm timer (mirrors Java: invalid.add(v -> if (!v) invalidTimer.disarm()))
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
            _extraMemory.ResetCache();
            _game.HandleRefresh(useFakeDailyLogin: true);
            _logger.LogWarning("Triggering refresh: stuck at loading screen for too long");
        }
    }

    private bool TryInstall()
    {
        if (!_game.IsValid)
            return true;

        if (_game is FridaGameApi fridaApi)
            return TryInstallFromFrida(fridaApi);

        return true;
    }

    private bool TryInstallFromFrida(FridaGameApi fridaApi)
    {
        fridaApi.RefreshStatus();
        var status = fridaApi.CurrentStatus;
        if (status?.Ready != true)
            return true;

        var mainApplicationAddress = FridaBridgeStatus.ParsePtr(status.MainApplicationAddress);
        var mainAddress = FridaBridgeStatus.ParsePtr(status.MainAddress);
        var screenManagerAddress = FridaBridgeStatus.ParsePtr(status.ScreenManager);
        if (mainApplicationAddress == 0 || mainAddress == 0 || screenManagerAddress == 0)
            return true;

        if (_installedScreenManager == screenManagerAddress && !_addresses.IsInvalid)
            return false;

        _addresses.SetMainApplicationAddress(mainApplicationAddress);
        _addresses.SetMainAddress(mainAddress);
        _addresses.SetScreenManagerAddress(screenManagerAddress);
        _installedScreenManager = screenManagerAddress;

        var connectionManager = FridaBridgeStatus.ParsePtr(status.ConnectionManager);
        if (connectionManager != 0)
            _addresses.SetConnectionManagerAddress(connectionManager);

        _addresses.SetHeroInfoAddress(0);

        var settingsAddress = _extraMemory.SearchClassClosure(SettingsPattern);
        if (settingsAddress != 0)
            _addresses.SetSettingsAddress(settingsAddress);

        _logger.LogInformation(
            "BotInstaller connected (Frida): screenManager=0x{ScreenManager:X}, main=0x{Main:X}",
            screenManagerAddress,
            mainAddress);

        return false;
    }

    private void ValidateInstalledAddresses()
    {
        if (_game.Mode == GameApiMode.FridaClient)
        {
            if (!_game.IsValid)
            {
                _logger.LogDebug("Frida no longer ready — invalidating addresses");
                _installedScreenManager = 0;
                _addresses.MarkInvalid();
                return;
            }

            return;
        }

        if (_game.ReadLong(_addresses.MainApplicationAddress + 1344) != _addresses.MainAddress)
        {
            _addresses.MarkInvalid();
            return;
        }

        if (_addresses.HeroInfoAddress == 0)
            CheckUserData();

        if (_addresses.ConnectionManagerAddress == 0)
        {
            var connMgr = _game.ReadLong(_addresses.MainAddress + 560);
            if (connMgr != 0)
                _addresses.SetConnectionManagerAddress(connMgr);
        }
    }

    private void CheckUserData()
    {
        var heroStatic = _game.ReadLong(_addresses.ScreenManagerAddress + 240);
        var heroId = _game.ReadInt(heroStatic + 56);
        if (heroId == 0)
            return;

        var address = _extraMemory.SearchClassClosure(closure =>
        {
            if (heroId != _game.ReadInt(closure + 0x30))
                return false;

            var level = _game.ReadInt(closure + 0x34);
            var boolVal = _game.ReadInt(closure + 0x3c);
            var val = _game.ReadInt(closure + 0x40);
            var cargo = ReadBindableInt(closure, 0x148);
            var maxCargo = ReadBindableInt(closure, 0x150);

            return level is >= 0 and <= 100
                   && boolVal is 1 or 2
                   && val == 0
                   && cargo >= 0
                   && maxCargo is >= 100 and < 100_000;
        });

        if (address != 0)
            _addresses.SetHeroInfoAddress(address);
    }

    private int ReadBindableInt(long closure, int offset)
    {
        var bindable = _game.ReadLong(closure + offset);
        return bindable == 0 ? 0 : _game.ReadInt(bindable + 0x10);
    }

    private static bool SettingsPattern(long address, IGameConnection game) =>
        game.ReadInt(address + 48) == -1
        && game.ReadInt(address + 52) == 0
        && game.ReadInt(address + 56) == 2
        && game.ReadInt(address + 60) == 1;

    private bool SettingsPattern(long address) => SettingsPattern(address, _game);

    private void ArmInvalidTimer(long delayMs)
    {
        _invalidTimerArmed = true;
        _invalidTimerDeadlineMs = Environment.TickCount64 + delayMs;
    }

    public void Dispose() { }
}
