#include "JniInvoke.h"

#include "JniHost.h"

namespace darkbot::jni {

jmethodID GetInstanceMethod(jobject instance, const char* name, const char* signature) {
    if (instance == nullptr) {
        return nullptr;
    }

    JNIEnv* env = Env();
    const jclass cls = env->GetObjectClass(instance);
    if (cls == nullptr) {
        return nullptr;
    }

    const jmethodID method = env->GetMethodID(cls, name, signature);
    env->DeleteLocalRef(cls);
    return method;
}

}  // namespace darkbot::jni
