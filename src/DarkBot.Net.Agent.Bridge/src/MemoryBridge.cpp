#include "DarkBotBridge.h"

#include "JniHost.h"
#include "JniInvoke.h"

#include <cstring>

namespace {

jobjectArray GetDarkMemProcesses(JNIEnv* env, jobject darkMem, jmethodID method) {
    return static_cast<jobjectArray>(env->CallObjectMethod(darkMem, method));
}

}  // namespace

int bridge_init(const char* lib_dir, const char* classes_dir, const char* working_dir) {
    if (lib_dir == nullptr || classes_dir == nullptr) {
        darkbot::jni::SetError("lib_dir and classes_dir are required");
        return -1;
    }

    std::string error;
    if (!darkbot::jni::Init(lib_dir, classes_dir, working_dir, error)) {
        return -2;
    }

    return 0;
}

void bridge_shutdown() {
    darkbot::jni::Shutdown();
}

int bridge_get_version() {
    if (!darkbot::jni::IsReady()) {
        darkbot::jni::SetError("bridge is not initialized");
        return -1;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "getVersion", "()I");
    if (method == nullptr) {
        darkbot::jni::SetError("DarkMem.getVersion not found");
        return -1;
    }

    return env->CallIntMethod(darkMem, method);
}

void bridge_open_process(long pid) {
    if (!darkbot::jni::IsReady()) {
        darkbot::jni::SetError("bridge is not initialized");
        return;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "openProcess", "(J)V");
    if (method == nullptr) {
        darkbot::jni::SetError("DarkMem.openProcess not found");
        return;
    }

    env->CallVoidMethod(darkMem, method, static_cast<jlong>(pid));
}

int bridge_read_int(long address) {
    if (!darkbot::jni::IsReady()) {
        darkbot::jni::SetError("bridge is not initialized");
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "readInt", "(J)I");
    if (method == nullptr) {
        darkbot::jni::SetError("DarkMem.readInt not found");
        return 0;
    }

    return env->CallIntMethod(darkMem, method, static_cast<jlong>(address));
}

long bridge_read_long(long address) {
    if (!darkbot::jni::IsReady()) {
        darkbot::jni::SetError("bridge is not initialized");
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "readLong", "(J)J");
    if (method == nullptr) {
        darkbot::jni::SetError("DarkMem.readLong not found");
        return 0;
    }

    return static_cast<long>(env->CallLongMethod(darkMem, method, static_cast<jlong>(address)));
}

double bridge_read_double(long address) {
    if (!darkbot::jni::IsReady()) {
        darkbot::jni::SetError("bridge is not initialized");
        return 0.0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "readDouble", "(J)D");
    if (method == nullptr) {
        darkbot::jni::SetError("DarkMem.readDouble not found");
        return 0.0;
    }

    return env->CallDoubleMethod(darkMem, method, static_cast<jlong>(address));
}

int bridge_get_last_error(char* buffer, int buffer_size) {
    if (buffer == nullptr || buffer_size <= 0) {
        return 0;
    }

    const std::string& message = darkbot::jni::LastError();
    const int toCopy = static_cast<int>(message.size());
    const int maxCopy = buffer_size - 1;
    const int copied = toCopy < maxCopy ? toCopy : maxCopy;
    if (copied > 0) {
        std::memcpy(buffer, message.data(), static_cast<size_t>(copied));
    }
    buffer[copied] = '\0';
    return copied;
}

int bridge_darkmem_get_process_count() {
    if (!darkbot::jni::IsReady()) {
        darkbot::jni::SetError("bridge is not initialized");
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "getProcesses", "()[Leu/darkbot/api/DarkMem$Proc;");
    if (method == nullptr) {
        darkbot::jni::SetError("DarkMem.getProcesses not found");
        return 0;
    }

    jobjectArray processes = GetDarkMemProcesses(env, darkMem, method);
    if (processes == nullptr) {
        return 0;
    }

    const jsize count = env->GetArrayLength(processes);
    env->DeleteLocalRef(processes);
    return static_cast<int>(count);
}

int bridge_darkmem_get_process_pid(int index) {
    if (!darkbot::jni::IsReady()) {
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "getProcesses", "()[Leu/darkbot/api/DarkMem$Proc;");
    if (method == nullptr) {
        return 0;
    }

    jobjectArray processes = GetDarkMemProcesses(env, darkMem, method);
    if (processes == nullptr || index < 0 || index >= env->GetArrayLength(processes)) {
        if (processes != nullptr) {
            env->DeleteLocalRef(processes);
        }
        return 0;
    }

    jobject proc = env->GetObjectArrayElement(processes, index);
    const jmethodID pidMethod = darkbot::jni::GetInstanceMethod(proc, "getPid", "()I");
    const int pid = pidMethod != nullptr ? env->CallIntMethod(proc, pidMethod) : 0;
    env->DeleteLocalRef(proc);
    env->DeleteLocalRef(processes);
    return pid;
}

int bridge_darkmem_get_process_name(int index, char* buffer, int buffer_size) {
    if (buffer == nullptr || buffer_size <= 0) {
        return 0;
    }

    buffer[0] = '\0';
    if (!darkbot::jni::IsReady()) {
        return 0;
    }

    JNIEnv* env = darkbot::jni::Env();
    jobject darkMem = darkbot::jni::DarkMem();
    const jmethodID method = darkbot::jni::GetInstanceMethod(darkMem, "getProcesses", "()[Leu/darkbot/api/DarkMem$Proc;");
    if (method == nullptr) {
        return 0;
    }

    jobjectArray processes = GetDarkMemProcesses(env, darkMem, method);
    if (processes == nullptr || index < 0 || index >= env->GetArrayLength(processes)) {
        if (processes != nullptr) {
            env->DeleteLocalRef(processes);
        }
        return 0;
    }

    jobject proc = env->GetObjectArrayElement(processes, index);
    const jmethodID nameMethod = darkbot::jni::GetInstanceMethod(proc, "getName", "()Ljava/lang/String;");
    jstring jName = nameMethod != nullptr
        ? static_cast<jstring>(env->CallObjectMethod(proc, nameMethod))
        : nullptr;
    env->DeleteLocalRef(proc);
    env->DeleteLocalRef(processes);

    if (jName == nullptr) {
        return 0;
    }

    const char* utf = env->GetStringUTFChars(jName, nullptr);
    const int toCopy = static_cast<int>(std::strlen(utf));
    const int maxCopy = buffer_size - 1;
    const int copied = toCopy < maxCopy ? toCopy : maxCopy;
    if (copied > 0) {
        std::memcpy(buffer, utf, static_cast<size_t>(copied));
    }
    buffer[copied] = '\0';
    env->ReleaseStringUTFChars(jName, utf);
    env->DeleteLocalRef(jName);
    return copied;
}
