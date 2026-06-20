#include "DarkBotBridge.h"

#include "JniHost.h"
#include "JniInvoke.h"

#include <algorithm>
#include <atomic>
#include <chrono>
#include <cstring>
#include <mutex>
#include <string>
#include <thread>

#ifdef _WIN32
#include <objbase.h>
#endif

namespace {

bool RequireKekka() {
    if (!darkbot::jni::IsKekkaAvailable()) {
        darkbot::jni::SetError("KekkaPlayer is not available");
        return false;
    }
    return true;
}

jobject KekkaInstance() {
    return darkbot::jni::KekkaPlayer();
}

jstring ToJString(JNIEnv* env, const char* value) {
    return env->NewStringUTF(value != nullptr ? value : "");
}

enum class WindowLoopState {
    Idle = 0,
    Running = 1,
    Exited = 2,
    Failed = 3,
};

std::atomic<int> gWindowLoopState{static_cast<int>(WindowLoopState::Idle)};
std::atomic<long long> gWindowLoopDurationMs{0};
std::mutex gWindowLoopDetailMutex;
std::string gWindowLoopDetail;

void SetWindowLoopDetail(const std::string& detail) {
    std::lock_guard<std::mutex> lock(gWindowLoopDetailMutex);
    gWindowLoopDetail = detail;
    darkbot::jni::SetError(detail);
}

void SetWindowLoopFailed(const std::string& detail) {
    gWindowLoopState.store(static_cast<int>(WindowLoopState::Failed));
    SetWindowLoopDetail(detail);
}

bool CheckJniException(JNIEnv* env, const char* context) {
    if (env == nullptr || !env->ExceptionCheck()) {
        return true;
    }

    const std::string detail = darkbot::jni::DescribePendingException(env);
    SetWindowLoopDetail(
        std::string(context) + (detail.empty() ? " failed with a Java exception" : (": " + detail)));
    return false;
}

void CopyWindowLoopDetail(char* buffer, int buffer_size) {
    if (buffer == nullptr || buffer_size <= 0) {
        return;
    }

    std::lock_guard<std::mutex> lock(gWindowLoopDetailMutex);
    const size_t copyLength = gWindowLoopDetail.size() < static_cast<size_t>(buffer_size - 1)
        ? gWindowLoopDetail.size()
        : static_cast<size_t>(buffer_size - 1);
    if (copyLength > 0) {
        std::memcpy(buffer, gWindowLoopDetail.data(), copyLength);
    }
    buffer[copyLength] = '\0';
}

std::string BuildImmediateExitMessage(long long durationMs, long lastInternetRead) {
    std::string message =
        "Flash window closed immediately after " + std::to_string(durationMs) +
        " ms (message loop exited before the game could load).";

    if (lastInternetRead == 0) {
        message +=
            " Flash did not download any resources — check DarkFlash.ocx path, COM/ActiveX registration,"
            " and that KekkaPlayer.dll matches your OS bitness.";
    } else {
        message += " Some network activity was detected, but the client still closed early.";
    }

    return message;
}

}  // namespace

int bridge_kekka_is_available() {
    return darkbot::jni::IsKekkaAvailable() ? 1 : 0;
}

int bridge_kekka_get_version() {
    if (!RequireKekka()) {
        return -1;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "getVersion", "()I");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.getVersion not found");
        return -1;
    }

    return env->CallIntMethod(kekka, method);
}

int bridge_kekka_is_valid() {
    if (!RequireKekka()) {
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "isValid", "()Z");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.isValid not found");
        return 0;
    }

    return env->CallBooleanMethod(kekka, method) ? 1 : 0;
}

void bridge_kekka_set_flash_ocx_path(const char* path) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "setFlashOcxPath", "(Ljava/lang/String;)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.setFlashOcxPath not found");
        return;
    }

    jstring jPath = ToJString(env, path);
    env->CallVoidMethod(kekka, method, jPath);
    env->DeleteLocalRef(jPath);
    if (!CheckJniException(env, "KekkaPlayer.setFlashOcxPath")) {
        return;
    }

    if (path == nullptr || path[0] == '\0') {
        SetWindowLoopDetail("KekkaPlayer.setFlashOcxPath received an empty path");
    }
}

void bridge_kekka_set_data(
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(
        kekka,
        "setData",
        "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.setData not found");
        return;
    }

    jstring jUrl = ToJString(env, url);
    jstring jSid = ToJString(env, sid);
    jstring jPreloader = ToJString(env, preloader);
    jstring jVars = ToJString(env, vars);
    env->CallVoidMethod(kekka, method, jUrl, jSid, jPreloader, jVars);
    env->DeleteLocalRef(jUrl);
    env->DeleteLocalRef(jSid);
    env->DeleteLocalRef(jPreloader);
    env->DeleteLocalRef(jVars);
    CheckJniException(env, "KekkaPlayer.setData");
}

bool CallVoidStringMethodOnThread(
    JNIEnv* env,
    jobject kekka,
    const char* methodName,
    const char* value,
    const char* context) {
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, methodName, "(Ljava/lang/String;)V");
    if (method == nullptr) {
        SetWindowLoopFailed(std::string("KekkaPlayer.") + methodName + " native method not found in JNI bridge");
        return false;
    }

    jstring jValue = ToJString(env, value);
    env->CallVoidMethod(kekka, method, jValue);
    env->DeleteLocalRef(jValue);
    return CheckJniException(env, context);
}

bool CallVoidIntIntMethodOnThread(
    JNIEnv* env,
    jobject kekka,
    const char* methodName,
    int first,
    int second,
    const char* context) {
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, methodName, "(II)V");
    if (method == nullptr) {
        SetWindowLoopFailed(std::string("KekkaPlayer.") + methodName + " native method not found in JNI bridge");
        return false;
    }

    env->CallVoidMethod(kekka, method, first, second);
    return CheckJniException(env, context);
}

bool CallSetDataOnThread(
    JNIEnv* env,
    jobject kekka,
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars) {
    const jmethodID method = darkbot::jni::GetInstanceMethod(
        kekka,
        "setData",
        "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;)V");
    if (method == nullptr) {
        SetWindowLoopFailed("KekkaPlayer.setData native method not found in JNI bridge");
        return false;
    }

    jstring jUrl = ToJString(env, url);
    jstring jSid = ToJString(env, sid);
    jstring jPreloader = ToJString(env, preloader);
    jstring jVars = ToJString(env, vars);
    env->CallVoidMethod(kekka, method, jUrl, jSid, jPreloader, jVars);
    env->DeleteLocalRef(jUrl);
    env->DeleteLocalRef(jSid);
    env->DeleteLocalRef(jPreloader);
    env->DeleteLocalRef(jVars);
    return CheckJniException(env, "KekkaPlayer.setData");
}

struct KekkaWindowLaunchConfig {
    const char* url = nullptr;
    const char* sid = nullptr;
    const char* preloader = nullptr;
    const char* vars = nullptr;
    const char* flashOcxPath = nullptr;
    int width = 0;
    int height = 0;
    int minClientWidth = 0;
    int minClientHeight = 0;
    int proxyPort = 0;
};

bool ConfigureKekkaOnThread(JNIEnv* env, jobject kekka, const KekkaWindowLaunchConfig& config) {
    if (config.flashOcxPath != nullptr && config.flashOcxPath[0] != '\0' &&
        !CallVoidStringMethodOnThread(
            env,
            kekka,
            "setFlashOcxPath",
            config.flashOcxPath,
            "KekkaPlayer.setFlashOcxPath")) {
        return false;
    }

    if (config.minClientWidth > 0 && config.minClientHeight > 0 &&
        !CallVoidIntIntMethodOnThread(
            env,
            kekka,
            "setMinClientSize",
            config.minClientWidth,
            config.minClientHeight,
            "KekkaPlayer.setMinClientSize")) {
        return false;
    }

    if (config.width > 0 && config.height > 0 &&
        !CallVoidIntIntMethodOnThread(
            env,
            kekka,
            "setSize",
            config.width,
            config.height,
            "KekkaPlayer.setSize")) {
        return false;
    }

    if (config.proxyPort > 0) {
        const jmethodID proxyMethod = darkbot::jni::GetInstanceMethod(kekka, "setLocalProxy", "(I)V");
        if (proxyMethod == nullptr) {
            SetWindowLoopFailed("KekkaPlayer.setLocalProxy native method not found in JNI bridge");
            return false;
        }

        env->CallVoidMethod(kekka, proxyMethod, config.proxyPort);
        if (!CheckJniException(env, "KekkaPlayer.setLocalProxy")) {
            return false;
        }
    }

    if (config.url != nullptr && config.sid != nullptr && config.preloader != nullptr && config.vars != nullptr &&
        config.url[0] != '\0' && config.sid[0] != '\0' && config.preloader[0] != '\0') {
        return CallSetDataOnThread(env, kekka, config.url, config.sid, config.preloader, config.vars);
    }

    return true;
}

void RunKekkaWindowThread(JavaVM* jvm) {
    gWindowLoopState.store(static_cast<int>(WindowLoopState::Running));
    gWindowLoopDurationMs.store(0);
    SetWindowLoopDetail({});

    const auto startedAt = std::chrono::steady_clock::now();
#ifdef _WIN32
    const HRESULT comInit = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    const bool comInitialized = comInit == S_OK || comInit == S_FALSE;
    if (comInit == RPC_E_CHANGED_MODE) {
        SetWindowLoopFailed(
            "KekkaPlayer API thread is already in MTA COM mode (RPC_E_CHANGED_MODE). "
            "Flash ActiveX requires a dedicated STA thread — avoid initializing COM on this thread before launch.");
        return;
    }
    if (FAILED(comInit)) {
        SetWindowLoopFailed(
            "CoInitializeEx failed for KekkaPlayer API thread (HRESULT=" +
            std::to_string(static_cast<unsigned long>(comInit)) +
            "). Flash ActiveX requires STA COM apartment.");
        return;
    }
#endif

    JNIEnv* env = nullptr;
    JavaVMAttachArgs attachArgs{};
    attachArgs.version = JNI_VERSION_1_8;
    attachArgs.name = const_cast<char*>("KekkaPlayer-API");
    attachArgs.group = nullptr;
    if (jvm->AttachCurrentThread(reinterpret_cast<void**>(&env), &attachArgs) != JNI_OK || env == nullptr) {
        SetWindowLoopFailed("Failed to attach KekkaPlayer API thread to embedded JVM");
#ifdef _WIN32
        if (comInitialized) {
            CoUninitialize();
        }
#endif
        return;
    }

    jobject kekka = darkbot::jni::KekkaPlayer();
    const jmethodID createWindowMethod = darkbot::jni::GetInstanceMethod(kekka, "createWindow", "()V");
    if (createWindowMethod == nullptr) {
        SetWindowLoopFailed("KekkaPlayer.createWindow native method not found in JNI bridge");
#ifdef _WIN32
        if (comInitialized) {
            CoUninitialize();
        }
#endif
        return;
    }

    env->CallVoidMethod(kekka, createWindowMethod);
    const bool jniOk = CheckJniException(env, "KekkaPlayer.createWindow");

    const auto durationMs = std::chrono::duration_cast<std::chrono::milliseconds>(
        std::chrono::steady_clock::now() - startedAt).count();
    gWindowLoopDurationMs.store(durationMs);

    long lastInternetRead = 0;
    const jmethodID lastReadMethod =
        darkbot::jni::GetInstanceMethod(kekka, "lastInternetReadTime", "()J");
    if (lastReadMethod != nullptr) {
        lastInternetRead = static_cast<long>(env->CallLongMethod(kekka, lastReadMethod));
        if (env->ExceptionCheck()) {
            env->ExceptionClear();
        }
    }

    if (!jniOk) {
        gWindowLoopState.store(static_cast<int>(WindowLoopState::Failed));
    } else if (durationMs < 3000) {
        SetWindowLoopFailed(BuildImmediateExitMessage(durationMs, lastInternetRead));
    } else {
        gWindowLoopState.store(static_cast<int>(WindowLoopState::Exited));
        SetWindowLoopDetail("Flash window closed after " + std::to_string(durationMs) + " ms");
    }

    // Keep the API thread attached to the JVM, matching Java's long-lived "API thread".
#ifdef _WIN32
    if (comInitialized) {
        CoUninitialize();
    }
#endif
}

struct WindowLaunchParams {
    JavaVM* jvm = nullptr;
    KekkaWindowLaunchConfig config{};
    std::string url;
    std::string sid;
    std::string preloader;
    std::string vars;
    std::string flashOcxPath;
};

void bridge_kekka_create_window() {
    if (!RequireKekka()) {
        return;
    }

    JavaVM* jvm = darkbot::jni::Jvm();
    if (jvm == nullptr) {
        darkbot::jni::SetError("JVM is not initialized");
        return;
    }

    std::thread([jvm]() { RunKekkaWindowThread(jvm); }).detach();
}

void bridge_kekka_launch_window(
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars) {
    bridge_kekka_launch_window_ex(url, sid, preloader, vars, nullptr, 0, 0, 0, 0, 0);
}

void bridge_kekka_launch_window_ex(
    const char* url,
    const char* sid,
    const char* preloader,
    const char* vars,
    const char* flash_ocx_path,
    int width,
    int height,
    int min_client_width,
    int min_client_height,
    int proxy_port) {
    if (!RequireKekka()) {
        return;
    }

    JavaVM* jvm = darkbot::jni::Jvm();
    if (jvm == nullptr) {
        darkbot::jni::SetError("JVM is not initialized");
        return;
    }

    auto* params = new WindowLaunchParams{
        jvm,
        {},
        url != nullptr ? url : "",
        sid != nullptr ? sid : "",
        preloader != nullptr ? preloader : "",
        vars != nullptr ? vars : "",
        flash_ocx_path != nullptr ? flash_ocx_path : "",
    };
    params->config.url = params->url.c_str();
    params->config.sid = params->sid.c_str();
    params->config.preloader = params->preloader.c_str();
    params->config.vars = params->vars.c_str();
    params->config.flashOcxPath = params->flashOcxPath.c_str();
    params->config.width = width;
    params->config.height = height;
    params->config.minClientWidth = min_client_width;
    params->config.minClientHeight = min_client_height;
    params->config.proxyPort = proxy_port;

    // Java GameAPIImpl: setFlashOcxPath / setMinClientSize / setSize / setData on JVM main thread;
    // only createWindow() runs on the dedicated API thread.
    JNIEnv* mainEnv = darkbot::jni::Env();
    if (mainEnv == nullptr) {
        darkbot::jni::SetError("JVM main thread JNIEnv unavailable for KekkaPlayer pre-window setup");
        delete params;
        return;
    }

    jobject kekka = darkbot::jni::KekkaPlayer();
    if (!ConfigureKekkaOnThread(mainEnv, kekka, params->config)) {
        delete params;
        return;
    }

    std::thread([params]() {
        RunKekkaWindowThread(params->jvm);
        delete params;
    }).detach();
}

int bridge_kekka_get_window_loop_state() {
    return gWindowLoopState.load();
}

long bridge_kekka_get_window_loop_duration_ms() {
    return gWindowLoopDurationMs.load();
}

int bridge_kekka_get_window_loop_detail(char* buffer, int buffer_size) {
    CopyWindowLoopDetail(buffer, buffer_size);
    std::lock_guard<std::mutex> lock(gWindowLoopDetailMutex);
    return static_cast<int>(gWindowLoopDetail.size());
}

void bridge_kekka_set_min_client_size(int width, int height) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "setMinClientSize", "(II)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.setMinClientSize not found");
        return;
    }

    env->CallVoidMethod(kekka, method, width, height);
}

void bridge_kekka_set_size(int width, int height) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "setSize", "(II)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.setSize not found");
        return;
    }

    env->CallVoidMethod(kekka, method, width, height);
}

void bridge_kekka_set_local_proxy(int port) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "setLocalProxy", "(I)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.setLocalProxy not found");
        return;
    }

    env->CallVoidMethod(kekka, method, port);
}

void bridge_kekka_reload() {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "reload", "()V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.reload not found");
        return;
    }

    env->CallVoidMethod(kekka, method);
}

void bridge_kekka_set_visible(int visible) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "setVisible", "(Z)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.setVisible not found");
        return;
    }

    env->CallVoidMethod(kekka, method, visible ? JNI_TRUE : JNI_FALSE);
}

long bridge_kekka_last_internet_read_time() {
    if (!RequireKekka()) {
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "lastInternetReadTime", "()J");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.lastInternetReadTime not found");
        return 0;
    }

    return static_cast<long>(env->CallLongMethod(kekka, method));
}

void bridge_kekka_clear_cache(const char* pattern) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "clearCache", "(Ljava/lang/String;)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.clearCache not found");
        return;
    }

    jstring jPattern = ToJString(env, pattern);
    env->CallVoidMethod(kekka, method, jPattern);
    env->DeleteLocalRef(jPattern);
}

void bridge_kekka_move_ship(long screenManager, long x, long y, long collectableAdr) {
    if (!RequireKekka()) {
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "moveShip", "(JJJJ)V");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.moveShip not found");
        return;
    }

    env->CallVoidMethod(
        kekka,
        method,
        static_cast<jlong>(screenManager),
        static_cast<jlong>(x),
        static_cast<jlong>(y),
        static_cast<jlong>(collectableAdr));
}

int bridge_kekka_read_int(long address) {
    if (!RequireKekka()) {
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "readInt", "(J)I");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.readInt not found");
        return 0;
    }

    return env->CallIntMethod(kekka, method, static_cast<jlong>(address));
}

long bridge_kekka_read_long(long address) {
    if (!RequireKekka()) {
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "readLong", "(J)J");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.readLong not found");
        return 0;
    }

    return static_cast<long>(env->CallLongMethod(kekka, method, static_cast<jlong>(address)));
}

double bridge_kekka_read_double(long address) {
    if (!RequireKekka()) {
        return 0.0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "readDouble", "(J)D");
    if (method == nullptr) {
        darkbot::jni::SetError("KekkaPlayer.readDouble not found");
        return 0.0;
    }

    return env->CallDoubleMethod(kekka, method, static_cast<jlong>(address));
}

long bridge_kekka_query_bytes(const unsigned char* pattern, int pattern_length) {
    if (!RequireKekka()) {
        return 0;
    }

    if (pattern == nullptr || pattern_length <= 0) {
        darkbot::jni::SetError("Invalid pattern for queryBytes");
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jbyteArray jPattern = env->NewByteArray(pattern_length);
    env->SetByteArrayRegion(jPattern, 0, pattern_length, reinterpret_cast<const jbyte*>(pattern));

    jobject kekka = KekkaInstance();
    const jmethodID method = darkbot::jni::GetInstanceMethod(kekka, "queryBytes", "([B)J");
    if (method == nullptr) {
        env->DeleteLocalRef(jPattern);
        darkbot::jni::SetError("KekkaPlayer.queryBytes not found");
        return 0;
    }

    const jlong result = env->CallLongMethod(kekka, method, jPattern);
    env->DeleteLocalRef(jPattern);
    return static_cast<long>(result);
}
