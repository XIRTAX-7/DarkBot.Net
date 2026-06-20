using System.Text;
using DarkBot.Net.Agent.Windows.Game;
using DarkBot.Net.Agent.Windows.Memory;

namespace DarkBot.Net.Agent.Windows.Bridge;

/// <summary>JNI shim surface for KekkaPlayer.dll (in-process Flash client).</summary>
public sealed class NativeKekkaBridge : INativeMemory
{
    private readonly NativeGameBridge _owner;

    internal NativeKekkaBridge(NativeGameBridge owner) => _owner = owner;

    public bool IsAvailable
    {
        get
        {
            if (!_owner.IsInitialized)
                return false;

            return Native.DarkBotBridgeNative.bridge_kekka_is_available() == 1;
        }
    }

    public int Version
    {
        get
        {
            _owner.EnsureInitialized();
            return Native.DarkBotBridgeNative.bridge_kekka_get_version();
        }
    }

    public bool IsValid
    {
        get
        {
            if (!_owner.IsInitialized)
                return false;

            return Native.DarkBotBridgeNative.bridge_kekka_is_valid() == 1;
        }
    }

    public void SetFlashOcxPath(string path)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_set_flash_ocx_path(path);
    }

    public void SetData(string url, string sid, string preloader, string vars)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_set_data(url, sid, preloader, vars);
    }

    public void CreateWindow()
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_create_window();
    }

    public void LaunchWindow(
        string url,
        string sid,
        string preloader,
        string vars,
        string flashOcxPath,
        int width,
        int height,
        int minClientWidth,
        int minClientHeight,
        int proxyPort)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_launch_window_ex(
            url,
            sid,
            preloader,
            vars,
            flashOcxPath,
            width,
            height,
            minClientWidth,
            minClientHeight,
            proxyPort);
    }

    public KekkaPlayerWindowStatus GetWindowStatus()
    {
        if (!_owner.IsInitialized)
        {
            return new KekkaPlayerWindowStatus
            {
                State = KekkaPlayerWindowLoopState.Idle,
            };
        }

        var buffer = new byte[1024];
        Native.DarkBotBridgeNative.bridge_kekka_get_window_loop_detail(buffer, buffer.Length);
        var detailLength = Array.IndexOf(buffer, (byte)0);
        if (detailLength < 0)
            detailLength = buffer.Length;

        return new KekkaPlayerWindowStatus
        {
            State = (KekkaPlayerWindowLoopState)Native.DarkBotBridgeNative.bridge_kekka_get_window_loop_state(),
            DurationMs = Native.DarkBotBridgeNative.bridge_kekka_get_window_loop_duration_ms(),
            Detail = detailLength > 0 ? Encoding.UTF8.GetString(buffer, 0, detailLength) : string.Empty,
        };
    }

    public void SetSize(int width, int height)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_set_size(width, height);
    }

    public void SetMinClientSize(int width, int height)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_set_min_client_size(width, height);
    }

    public void SetLocalProxy(int port)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_set_local_proxy(port);
    }

    public void Reload()
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_reload();
    }

    public void SetVisible(bool visible)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_set_visible(visible ? 1 : 0);
    }

    public long LastInternetReadTime()
    {
        _owner.EnsureInitialized();
        return Native.DarkBotBridgeNative.bridge_kekka_last_internet_read_time();
    }

    public void ClearCache(string pattern)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_clear_cache(pattern);
    }

    public void MoveShip(long screenManager, long x, long y, long collectableAddress = 0)
    {
        _owner.EnsureInitialized();
        Native.DarkBotBridgeNative.bridge_kekka_move_ship(screenManager, x, y, collectableAddress);
    }

    public int ReadInt(long address)
    {
        _owner.EnsureInitialized();
        return Native.DarkBotBridgeNative.bridge_kekka_read_int(address);
    }

    public long ReadLong(long address)
    {
        _owner.EnsureInitialized();
        return Native.DarkBotBridgeNative.bridge_kekka_read_long(address);
    }

    public double ReadDouble(long address)
    {
        _owner.EnsureInitialized();
        return Native.DarkBotBridgeNative.bridge_kekka_read_double(address);
    }

    public long QueryBytes(ReadOnlySpan<byte> pattern)
    {
        _owner.EnsureInitialized();
        var buffer = pattern.ToArray();
        return Native.DarkBotBridgeNative.bridge_kekka_query_bytes(buffer, buffer.Length);
    }

    public GameMemoryReader CreateReader() => new(this);

    public int ReadHeroHp(long shipAddress) => CreateReader().ReadHeroHp(shipAddress);
}
