#pragma once

#include <jni.h>
#include <string>

namespace darkbot::jni {

bool Init(const char* libDir, const char* classesDir, const char* workingDir, std::string& error);

void Shutdown();

bool IsReady();

bool IsKekkaAvailable();

JNIEnv* Env();

JavaVM* Jvm();

jobject DarkMem();

jobject KekkaPlayer();

void SetError(const std::string& message);

const std::string& LastError();

std::string DescribePendingException(JNIEnv* env);

}  // namespace darkbot::jni
