namespace DarkBot.Net.Core.Memory;

/// <summary>Port of BotInstaller address hooks.</summary>
public sealed class BotAddressRegistry
{
    private long _mainApplicationAddress;
    private long _mainAddress;
    private long _screenManagerAddress;
    private long _guiManagerAddress;
    private long _heroInfoAddress;
    private long _settingsAddress;
    private long _connectionManagerAddress;
    private bool _invalid = true;

    public long MainApplicationAddress => _mainApplicationAddress;
    public long MainAddress => _mainAddress;
    public long ScreenManagerAddress => _screenManagerAddress;
    public long GuiManagerAddress => _guiManagerAddress;
    public long HeroInfoAddress => _heroInfoAddress;
    public long SettingsAddress => _settingsAddress;
    public long ConnectionManagerAddress => _connectionManagerAddress;
    public bool IsInvalid => _invalid;

    public bool HasScreenManager => _screenManagerAddress != 0 && !_invalid;

    public event Action<long>? ScreenManagerAddressChanged;
    public event Action<long>? HeroInfoAddressChanged;
    public event Action? Invalidated;

    public void SetMainApplicationAddress(long address) => _mainApplicationAddress = address;

    public void SetMainAddress(long address) => _mainAddress = address;

    public void SetScreenManagerAddress(long address)
    {
        _screenManagerAddress = address;
        if (address != 0)
            _invalid = false;

        ScreenManagerAddressChanged?.Invoke(address);
    }

    public void SetGuiManagerAddress(long address) => _guiManagerAddress = address;

    public void SetHeroInfoAddress(long address)
    {
        _heroInfoAddress = address;
        HeroInfoAddressChanged?.Invoke(address);
    }

    public void SetSettingsAddress(long address) => _settingsAddress = address;

    public void SetConnectionManagerAddress(long address) => _connectionManagerAddress = address;

    public void MarkInvalid()
    {
        _invalid = true;
        _mainApplicationAddress = 0;
        _mainAddress = 0;
        _screenManagerAddress = 0;
        _guiManagerAddress = 0;
        _heroInfoAddress = 0;
        _settingsAddress = 0;
        _connectionManagerAddress = 0;
        Invalidated?.Invoke();
    }
}
