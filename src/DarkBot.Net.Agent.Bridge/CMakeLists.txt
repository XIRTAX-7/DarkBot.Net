cmake_minimum_required(VERSION 3.20)
project(DarkBotBridge VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(NOT DEFINED JAVA_HOME)
    if(DEFINED ENV{JAVA_HOME})
        set(JAVA_HOME "$ENV{JAVA_HOME}")
    else()
        message(FATAL_ERROR "JAVA_HOME is not set")
    endif()
endif()

set(JNI_INCLUDE_DIRS
    "${JAVA_HOME}/include"
    "${JAVA_HOME}/include/win32"
)

if(NOT EXISTS "${JAVA_HOME}/lib/jvm.lib")
    message(FATAL_ERROR "jvm.lib not found under JAVA_HOME=${JAVA_HOME}")
endif()

add_library(DarkBotBridge SHARED
    src/MemoryBridge.cpp
    src/JniHost.cpp
    src/JniInvoke.cpp
    src/KekkaShim.cpp
)

target_include_directories(DarkBotBridge PUBLIC include PRIVATE ${JNI_INCLUDE_DIRS})
target_compile_definitions(DarkBotBridge PRIVATE DARKBOT_BRIDGE_EXPORTS)
target_link_libraries(DarkBotBridge PRIVATE "${JAVA_HOME}/lib/jvm.lib")

if(WIN32)
    target_link_libraries(DarkBotBridge PRIVATE ole32)
endif()

if(MSVC)
    target_compile_options(DarkBotBridge PRIVATE /W4)
endif()
