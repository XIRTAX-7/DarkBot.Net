# Builds DarkBotBridge.dll (JNI host over DarkMemAPI.dll)
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$bridgeRoot = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $bridgeRoot '..\..\..')
$libDir = Join-Path $repoRoot 'lib'
$javaHome = $env:JAVA_HOME
if (-not $javaHome) {
    throw 'JAVA_HOME is not set'
}

$javac = Join-Path $javaHome 'bin\javac.exe'
if (-not (Test-Path $javac)) {
    throw "javac not found at $javac"
}

$classesDir = Join-Path $bridgeRoot 'build\classes'
$javaSrc = Join-Path $bridgeRoot 'java'
New-Item -ItemType Directory -Force -Path $classesDir | Out-Null

$darkBotJarCandidates = @(
    (Join-Path $libDir 'DarkBot.jar')
    'C:\DarkBot\DarkBot.jar'
)
$darkBotJar = $darkBotJarCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $darkBotJar) {
    throw 'DarkBot.jar not found — required to compile bridge (copy to lib/DarkBot.jar or install C:\DarkBot\DarkBot.jar)'
}

Write-Host "Compiling bridge Java (cp: $darkBotJar)..."
& $javac -encoding UTF-8 -classpath $darkBotJar -d $classesDir (Get-ChildItem -Path $javaSrc -Recurse -Filter '*.java' | ForEach-Object { $_.FullName })

$cmake = Get-Command cmake -ErrorAction SilentlyContinue
if (-not $cmake) {
    $cmakeCandidates = @(
        'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe',
        'C:\Program Files\CMake\bin\cmake.exe'
    )
    foreach ($candidate in $cmakeCandidates) {
        if (Test-Path $candidate) {
            $cmake = @{ Source = $candidate }
            break
        }
    }
}
if (-not $cmake) {
    throw 'cmake not found in PATH'
}

$buildDir = Join-Path $bridgeRoot 'build\native'
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

Write-Host "Configuring native bridge..."
& $cmake.Source -S $bridgeRoot -B $buildDir -G 'Visual Studio 18 2026' -A x64 -DCMAKE_BUILD_TYPE=$Configuration

Write-Host "Building native bridge ($Configuration)..."
& $cmake.Source --build $buildDir --config $Configuration

$builtDll = Join-Path $buildDir "$Configuration\DarkBotBridge.dll"
if (-not (Test-Path $builtDll)) {
    $builtDll = Join-Path $buildDir 'DarkBotBridge.dll'
}
if (-not (Test-Path $builtDll)) {
    throw "DarkBotBridge.dll was not produced"
}

New-Item -ItemType Directory -Force -Path $libDir | Out-Null
Copy-Item -Force $builtDll (Join-Path $libDir 'DarkBotBridge.dll')
Write-Host "Copied to $(Join-Path $libDir 'DarkBotBridge.dll')"
