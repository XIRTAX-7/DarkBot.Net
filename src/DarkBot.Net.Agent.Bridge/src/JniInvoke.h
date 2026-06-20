#pragma once

#include <jni.h>

namespace darkbot::jni {

jmethodID GetInstanceMethod(jobject instance, const char* name, const char* signature);

}  // namespace darkbot::jni
