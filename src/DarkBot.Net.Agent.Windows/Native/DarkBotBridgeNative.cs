using System.Runtime.InteropServices;
using System.Text;

namespace DarkBot.Net.Agent.Windows.Native;

internal static partial class DarkBotBridgeNative
{
    private const string DllName = "DarkBotBridge";

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int bridge_init(string libDir, string classesDir, string? workingDir);

    [LibraryImport(DllName)]
    internal static partial void bridge_shutdown();

    [LibraryImport(DllName)]
    internal static partial int bridge_get_version();

    [LibraryImport(DllName)]
    internal static partial void bridge_open_process(long pid);

    [LibraryImport(DllName)]
    internal static partial int bridge_read_int(long address);

    [LibraryImport(DllName)]
    internal static partial long bridge_read_long(long address);

    [LibraryImport(DllName)]
    internal static partial double bridge_read_double(long address);

    [LibraryImport(DllName)]
    internal static partial int bridge_darkmem_get_process_count();

    [LibraryImport(DllName)]
    internal static partial int bridge_darkmem_get_process_pid(int index);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int bridge_darkmem_get_process_name(int index, byte[] buffer, int bufferSize);

    [LibraryImport(DllName)]
    internal static partial int bridge_kekka_is_available();

    [LibraryImport(DllName)]
    internal static partial int bridge_kekka_get_version();

    [LibraryImport(DllName)]
    internal static partial int bridge_kekka_is_valid();

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void bridge_kekka_set_flash_ocx_path(string path);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void bridge_kekka_set_data(string url, string sid, string preloader, string vars);

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_create_window();

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void bridge_kekka_launch_window(string url, string sid, string preloader, string vars);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void bridge_kekka_launch_window_ex(
        string url,
        string sid,
        string preloader,
        string vars,
        string flashOcxPath,
        int width,
        int height,
        int minClientWidth,
        int minClientHeight,
        int proxyPort);

    [LibraryImport(DllName)]
    internal static partial int bridge_kekka_get_window_loop_state();

    [LibraryImport(DllName)]
    internal static partial long bridge_kekka_get_window_loop_duration_ms();

    [LibraryImport(DllName)]
    internal static partial int bridge_kekka_get_window_loop_detail(byte[] buffer, int bufferSize);

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_set_size(int width, int height);

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_set_min_client_size(int width, int height);

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_set_local_proxy(int port);

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_reload();

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_set_visible(int visible);

    [LibraryImport(DllName)]
    internal static partial long bridge_kekka_last_internet_read_time();

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void bridge_kekka_clear_cache(string pattern);

    [LibraryImport(DllName)]
    internal static partial void bridge_kekka_move_ship(long screenManager, long x, long y, long collectableAdr);

    [LibraryImport(DllName)]
    internal static partial int bridge_kekka_read_int(long address);

    [LibraryImport(DllName)]
    internal static partial long bridge_kekka_read_long(long address);

    [LibraryImport(DllName)]
    internal static partial double bridge_kekka_read_double(long address);

    [LibraryImport(DllName)]
    internal static partial long bridge_kekka_query_bytes(byte[] pattern, int patternLength);

    [LibraryImport(DllName)]
    internal static partial int bridge_get_last_error(byte[] buffer, int bufferSize);

    internal static string GetLastError()
    {
        var buffer = new byte[512];
        var length = bridge_get_last_error(buffer, buffer.Length);
        return length <= 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, length);
    }
}
