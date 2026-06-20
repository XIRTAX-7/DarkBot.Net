#pragma once

#ifdef _WIN32
#  ifdef DARKBOT_BRIDGE_EXPORTS
#    define DARKBOT_BRIDGE_API __declspec(dllexport)
#  else
#    define DARKBOT_BRIDGE_API __declspec(dllimport)
#  endif
#else
#  define DARKBOT_BRIDGE_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

/// Initialize embedded JVM, load DarkMem (+ KekkaPlayer when DLL is available).
/// @param lib_dir Directory containing native DLLs (java.library.path).
/// @param classes_dir Directory with compiled bridge Java classes (java.class.path).
/// @param working_dir JVM user.dir (verifier.jar, token). Null = parent of lib_dir when lib ends with /lib.
/// @return 0 on success, negative on failure.
DARKBOT_BRIDGE_API int bridge_init(const char* lib_dir, const char* classes_dir, const char* working_dir);

DARKBOT_BRIDGE_API void bridge_shutdown(void);

DARKBOT_BRIDGE_API int bridge_get_last_error(char* buffer, int buffer_size);

// --- DarkMem (attach / external process memory) ---

DARKBOT_BRIDGE_API int bridge_get_version(void);

DARKBOT_BRIDGE_API void bridge_open_process(long pid);

DARKBOT_BRIDGE_API int bridge_read_int(long address);

DARKBOT_BRIDGE_API long bridge_read_long(long address);

DARKBOT_BRIDGE_API double bridge_read_double(long address);

DARKBOT_BRIDGE_API int bridge_darkmem_get_process_count(void);

DARKBOT_BRIDGE_API int bridge_darkmem_get_process_pid(int index);

DARKBOT_BRIDGE_API int bridge_darkmem_get_process_name(int index, char* buffer, int buffer_size);

// --- KekkaPlayer (in-process Flash client) ---

DARKBOT_BRIDGE_API int bridge_kekka_is_available(void);

DARKBOT_BRIDGE_API int bridge_kekka_get_version(void);

DARKBOT_BRIDGE_API int bridge_kekka_is_valid(void);

DARKBOT_BRIDGE_API void bridge_kekka_set_flash_ocx_path(const char* path);

DARKBOT_BRIDGE_API void bridge_kekka_set_data(
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars);

DARKBOT_BRIDGE_API void bridge_kekka_create_window(void);

DARKBOT_BRIDGE_API void bridge_kekka_launch_window(
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars);

/// Full launch on the KekkaPlayer API thread (STA/COM): flash path, sizes, proxy, setData, createWindow.
DARKBOT_BRIDGE_API void bridge_kekka_launch_window_ex(
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars,
    const char* flash_ocx_path,
    int width,
    int height,
    int min_client_width,
    int min_client_height,
    int proxy_port);

DARKBOT_BRIDGE_API int bridge_kekka_get_window_loop_state(void);

DARKBOT_BRIDGE_API long bridge_kekka_get_window_loop_duration_ms(void);

DARKBOT_BRIDGE_API int bridge_kekka_get_window_loop_detail(char* buffer, int buffer_size);

DARKBOT_BRIDGE_API void bridge_kekka_set_size(int width, int height);

DARKBOT_BRIDGE_API void bridge_kekka_set_min_client_size(int width, int height);

DARKBOT_BRIDGE_API void bridge_kekka_set_local_proxy(int port);

DARKBOT_BRIDGE_API void bridge_kekka_reload(void);

DARKBOT_BRIDGE_API void bridge_kekka_set_visible(int visible);

DARKBOT_BRIDGE_API long bridge_kekka_last_internet_read_time(void);

DARKBOT_BRIDGE_API void bridge_kekka_clear_cache(const char* pattern);

DARKBOT_BRIDGE_API void bridge_kekka_move_ship(long screenManager, long x, long y, long collectableAdr);

DARKBOT_BRIDGE_API int bridge_kekka_read_int(long address);

DARKBOT_BRIDGE_API long bridge_kekka_read_long(long address);

DARKBOT_BRIDGE_API double bridge_kekka_read_double(long address);

DARKBOT_BRIDGE_API long bridge_kekka_query_bytes(const unsigned char* pattern, int pattern_length);

#ifdef __cplusplus
}
#endif
