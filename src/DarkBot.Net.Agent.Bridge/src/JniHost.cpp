#include "JniHost.h"

#include <cstring>
#include <vector>

namespace darkbot::jni {
namespace {

std::string ResolveWorkingDir(const char* libDir) {
    if (libDir == nullptr || libDir[0] == '\0') {
        return {};
    }

    std::string path = libDir;
    while (path.size() > 1 && (path.back() == '\\' || path.back() == '/')) {
        path.pop_back();
    }

    const size_t separator = path.find_last_of("\\/");
    if (separator == std::string::npos) {
        return path;
    }

    const std::string leaf = path.substr(separator + 1);
    if (_stricmp(leaf.c_str(), "lib") == 0) {
        return path.substr(0, separator);
    }

    return path;
}

std::string NormalizePathSeparators(std::string path) {
    for (char& ch : path) {
        if (ch == '\\') {
            ch = '/';
        }
    }
    return path;
}

std::string BuildKekkaLibraryOption(const char* libDir) {
    if (libDir == nullptr || libDir[0] == '\0') {
        return {};
    }

    std::string path = libDir;
    while (path.size() > 1 && (path.back() == '\\' || path.back() == '/')) {
        path.pop_back();
    }

    if (path.size() >= 4 && _stricmp(path.substr(path.size() - 4).c_str(), ".dll") == 0) {
        return std::string("-Ddarkbot.kekka.library=") + NormalizePathSeparators(path);
    }

    if (path.size() >= 3 && _stricmp(path.substr(path.size() - 3).c_str(), "lib") == 0) {
        path += "/KekkaPlayer.dll";
    } else {
        path += "/lib/KekkaPlayer.dll";
    }

    return std::string("-Ddarkbot.kekka.library=") + NormalizePathSeparators(path);
}

JavaVM* gJvm = nullptr;
JNIEnv* gEnv = nullptr;
jobject gDarkMem = nullptr;
jobject gKekkaPlayer = nullptr;
bool gKekkaAvailable = false;
std::string gLastError;

std::string FormatPendingException(JNIEnv* env) {
    if (env == nullptr || !env->ExceptionCheck()) {
        return {};
    }

    const jthrowable exception = env->ExceptionOccurred();
    env->ExceptionClear();
    if (exception == nullptr) {
        return {};
    }

    const jclass exceptionClass = env->GetObjectClass(exception);
    const jmethodID toString = exceptionClass != nullptr
        ? env->GetMethodID(exceptionClass, "toString", "()Ljava/lang/String;")
        : nullptr;
    const jstring message = toString != nullptr
        ? static_cast<jstring>(env->CallObjectMethod(exception, toString))
        : nullptr;

    std::string result;
    if (message != nullptr) {
        const char* utf = env->GetStringUTFChars(message, nullptr);
        if (utf != nullptr) {
            result = utf;
            env->ReleaseStringUTFChars(message, utf);
        }
        env->DeleteLocalRef(message);
    }

    const jmethodID getCause = exceptionClass != nullptr
        ? env->GetMethodID(exceptionClass, "getCause", "()Ljava/lang/Throwable;")
        : nullptr;
    if (getCause != nullptr) {
        const jthrowable cause = static_cast<jthrowable>(env->CallObjectMethod(exception, getCause));
        if (cause != nullptr && !env->ExceptionCheck()) {
            const jclass causeClass = env->GetObjectClass(cause);
            const jmethodID causeToString = causeClass != nullptr
                ? env->GetMethodID(causeClass, "toString", "()Ljava/lang/String;")
                : nullptr;
            const jstring causeMessage = causeToString != nullptr
                ? static_cast<jstring>(env->CallObjectMethod(cause, causeToString))
                : nullptr;
            if (causeMessage != nullptr) {
                const char* causeUtf = env->GetStringUTFChars(causeMessage, nullptr);
                if (causeUtf != nullptr) {
                    result += " | cause: ";
                    result += causeUtf;
                    env->ReleaseStringUTFChars(causeMessage, causeUtf);
                }
                env->DeleteLocalRef(causeMessage);
            }
            if (causeClass != nullptr) {
                env->DeleteLocalRef(causeClass);
            }
            env->DeleteLocalRef(cause);
        }
    }

    if (exceptionClass != nullptr) {
        env->DeleteLocalRef(exceptionClass);
    }
    env->DeleteLocalRef(exception);
    return result;
}

bool EnsureAuthApi(JNIEnv* env, std::string& error) {
    if (env == nullptr) {
        error = "JNI environment is null";
        return false;
    }

    jclass bootstrap = env->FindClass("eu/darkbot/bridge/KekkaAuthBootstrap");
    if (bootstrap == nullptr) {
        error = FormatPendingException(env);
        if (error.empty()) {
            error = "KekkaAuthBootstrap class not found";
        }
        return false;
    }

    const jmethodID ensureAuthApi = env->GetStaticMethodID(bootstrap, "ensureAuthApi", "()V");
    if (ensureAuthApi == nullptr) {
        error = FormatPendingException(env);
        if (error.empty()) {
            error = "KekkaAuthBootstrap.ensureAuthApi not found";
        }
        env->DeleteLocalRef(bootstrap);
        return false;
    }

    env->CallStaticVoidMethod(bootstrap, ensureAuthApi);
    env->DeleteLocalRef(bootstrap);

    if (env->ExceptionCheck()) {
        error = FormatPendingException(env);
        if (error.empty()) {
            error = "KekkaAuthBootstrap.ensureAuthApi failed";
        }
        return false;
    }

    error.clear();
    return true;
}

jobject CreateInstance(const char* className, std::string& error) {
    jclass cls = gEnv->FindClass(className);
    if (cls == nullptr) {
        const std::string javaError = FormatPendingException(gEnv);
        error = javaError.empty()
            ? std::string("Class not found: ") + className
            : std::string("Class not found: ") + className + ": " + javaError;
        return nullptr;
    }

    const jmethodID ctor = gEnv->GetMethodID(cls, "<init>", "()V");
    if (ctor == nullptr) {
        const std::string javaError = FormatPendingException(gEnv);
        error = javaError.empty()
            ? std::string("Constructor not found: ") + className
            : std::string("Constructor not found: ") + className + ": " + javaError;
        gEnv->DeleteLocalRef(cls);
        return nullptr;
    }

    jobject localInstance = gEnv->NewObject(cls, ctor);
    gEnv->DeleteLocalRef(cls);

    if (localInstance == nullptr || gEnv->ExceptionCheck()) {
        const std::string javaError = FormatPendingException(gEnv);
        error = javaError.empty()
            ? std::string("Failed to create instance: ") + className
            : std::string("Failed to create instance: ") + className + ": " + javaError;
        return nullptr;
    }

    return localInstance;
}

}  // namespace

void SetError(const std::string& message) {
    gLastError = message;
}

const std::string& LastError() {
    return gLastError;
}

bool Init(const char* libDir, const char* classesDir, const char* workingDir, std::string& error) {
    if (gJvm != nullptr) {
        error.clear();
        return true;
    }

    const std::string resolvedWorkingDir =
        workingDir != nullptr && workingDir[0] != '\0' ? workingDir : ResolveWorkingDir(libDir);

    std::vector<JavaVMOption> optionsStorage;
    std::vector<std::string> optionStrings;
    if (!resolvedWorkingDir.empty()) {
        optionStrings.emplace_back(std::string("-Duser.dir=") + NormalizePathSeparators(resolvedWorkingDir));
    }
    optionStrings.emplace_back(std::string("-Djava.library.path=") + NormalizePathSeparators(libDir));
    const std::string kekkaLibraryOption = BuildKekkaLibraryOption(libDir);
    if (!kekkaLibraryOption.empty()) {
        optionStrings.emplace_back(kekkaLibraryOption);
    }
    optionStrings.emplace_back(std::string("-Djava.class.path=") + NormalizePathSeparators(classesDir));

    optionsStorage.resize(optionStrings.size());
    for (size_t i = 0; i < optionStrings.size(); ++i) {
        optionsStorage[i].optionString = optionStrings[i].data();
    }

    JavaVMInitArgs vmArgs{};
    vmArgs.version = JNI_VERSION_1_8;
    vmArgs.nOptions = static_cast<jint>(optionsStorage.size());
    vmArgs.options = optionsStorage.data();
    vmArgs.ignoreUnrecognized = JNI_FALSE;

    const jint rc = JNI_CreateJavaVM(&gJvm, reinterpret_cast<void**>(&gEnv), &vmArgs);
    if (rc != JNI_OK || gEnv == nullptr) {
        error = "JNI_CreateJavaVM failed with code " + std::to_string(rc);
        SetError(error);
        return false;
    }

    std::string instanceError;
    jobject localDarkMem = CreateInstance("eu/darkbot/api/DarkMem", instanceError);
    if (localDarkMem == nullptr) {
        error = instanceError;
        SetError(error);
        gJvm->DestroyJavaVM();
        gJvm = nullptr;
        gEnv = nullptr;
        return false;
    }

    gDarkMem = gEnv->NewGlobalRef(localDarkMem);
    gEnv->DeleteLocalRef(localDarkMem);

    if (gDarkMem == nullptr) {
        error = "Failed to retain DarkMem global reference";
        SetError(error);
        gJvm->DestroyJavaVM();
        gJvm = nullptr;
        gEnv = nullptr;
        return false;
    }

    std::string authError;
    if (!EnsureAuthApi(gEnv, authError)) {
        error = "ensureAuthApi failed: " + authError;
        SetError(error);
        gEnv->DeleteGlobalRef(gDarkMem);
        gDarkMem = nullptr;
        gJvm->DestroyJavaVM();
        gJvm = nullptr;
        gEnv = nullptr;
        return false;
    }

    jobject localKekka = CreateInstance("eu/darkbot/api/KekkaPlayer", instanceError);
    if (localKekka != nullptr) {
        gKekkaPlayer = gEnv->NewGlobalRef(localKekka);
        gEnv->DeleteLocalRef(localKekka);
        gKekkaAvailable = gKekkaPlayer != nullptr;
    } else {
        gKekkaAvailable = false;
        SetError("KekkaPlayer unavailable: " + instanceError);
    }

    error.clear();
    if (gKekkaAvailable) {
        gLastError.clear();
    }
    return true;
}

void Shutdown() {
    if (gEnv != nullptr && gKekkaPlayer != nullptr) {
        gEnv->DeleteGlobalRef(gKekkaPlayer);
        gKekkaPlayer = nullptr;
    }

    if (gEnv != nullptr && gDarkMem != nullptr) {
        gEnv->DeleteGlobalRef(gDarkMem);
        gDarkMem = nullptr;
    }

    gKekkaAvailable = false;

    if (gJvm != nullptr) {
        gJvm->DestroyJavaVM();
        gJvm = nullptr;
        gEnv = nullptr;
    }
}

bool IsReady() {
    return gJvm != nullptr && gEnv != nullptr && gDarkMem != nullptr;
}

bool IsKekkaAvailable() {
    return IsReady() && gKekkaAvailable && gKekkaPlayer != nullptr;
}

JNIEnv* Env() {
    if (gJvm == nullptr) {
        return nullptr;
    }

    JNIEnv* env = nullptr;
    const jint rc = gJvm->GetEnv(reinterpret_cast<void**>(&env), JNI_VERSION_1_8);
    if (rc == JNI_EDETACHED) {
        JavaVMAttachArgs attachArgs{};
        attachArgs.version = JNI_VERSION_1_8;
        attachArgs.name = const_cast<char*>("DarkBot-Native");
        attachArgs.group = nullptr;
        if (gJvm->AttachCurrentThread(reinterpret_cast<void**>(&env), &attachArgs) != JNI_OK) {
            return nullptr;
        }
    } else if (rc != JNI_OK) {
        return nullptr;
    }

    return env;
}

JavaVM* Jvm() {
    return gJvm;
}

jobject DarkMem() {
    return gDarkMem;
}

jobject KekkaPlayer() {
    return gKekkaPlayer;
}

std::string DescribePendingException(JNIEnv* env) {
    return FormatPendingException(env);
}

}  // namespace darkbot::jni
