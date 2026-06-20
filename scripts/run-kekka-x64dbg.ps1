# Launch KekkaPlayerLauncher under x64dbg (x64) for createWindow crash analysis.
param(
    [string]$X64DbgDir = "C:\Users\rogoz\Downloads\snapshot_2026-05-27_12-11\release\x64",
    [string]$WorkingDir = "$PSScriptRoot\..\src\DarkBot.Net.Ui\bin\Debug\net10.0",
    [string]$PropertiesFile = "",
    [switch]$UseHeadless
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "kekka-launcher-common.ps1")

$X64DbgDir = (Resolve-Path $X64DbgDir).Path
$WorkingDir = (Resolve-Path $WorkingDir).Path
$x64dbg = Join-Path $X64DbgDir "x64dbg.exe"
$headlessExe = Join-Path $X64DbgDir "headless.exe"

if (-not (Test-Path $x64dbg)) {
    throw "x64dbg not found: $x64dbg"
}

$javaHome = $env:JAVA_HOME
if (-not $javaHome) { $javaHome = "C:\Program Files\Eclipse Adoptium\jdk-11.0.31.11-hotspot" }
$javac = Join-Path $javaHome "bin\javac.exe"
$java = (Resolve-Path (Join-Path $javaHome "bin\java.exe")).Path

$bridgeRoot = (Resolve-Path "$PSScriptRoot\..\src\DarkBot.Net.Agent.Bridge").Path
$classes = Join-Path $bridgeRoot "build\classes"
New-Item -ItemType Directory -Force -Path $classes | Out-Null
& $javac -encoding UTF-8 -classpath (Join-Path $WorkingDir "lib\DarkBot.jar") -d $classes (Get-ChildItem (Join-Path $bridgeRoot "java") -Recurse -Filter "*.java" | ForEach-Object FullName)

$libDir = Join-Path $WorkingDir "lib"
$kekkaDll = Join-Path $libDir "KekkaPlayer.dll"
$argFile = Join-Path $WorkingDir "kekka-launcher.jvmargs"
$batFile = Join-Path $WorkingDir "kekka-debug.cmd"
$dynamicScript = Join-Path $WorkingDir "x64dbg-kekka-run.txt"

$propsPath = $null
if ($PropertiesFile) {
    $propsPath = (Resolve-Path $PropertiesFile).Path
} elseif (Test-Path (Join-Path $WorkingDir "launch.properties")) {
    $propsPath = Join-Path $WorkingDir "launch.properties"
}

if (-not $propsPath) {
    throw "launch.properties not found in $WorkingDir - login via .NET UI first"
}

Write-Host "Reading session from: $propsPath"
Write-KekkaJvmArgFile -Path $argFile -WorkingDir $WorkingDir -LibDir $libDir -KekkaDll $kekkaDll -Classes $classes -PropertiesFile $propsPath
Write-KekkaDebugBatch -Path $batFile -WorkingDir $WorkingDir -JavaExe $java

# x64dbg init splits on commas — pass ONE command-line string (no commas), not @argfile in init.
$initArgs = @(
    "-Duser.dir=$(Convert-ToJavaPath $WorkingDir)",
    "-Djava.library.path=$(Convert-ToJavaPath $libDir)",
    "-Ddarkbot.kekka.library=$(Convert-ToJavaPath $kekkaDll)",
    "-cp",
    (Build-KekkaClasspath -Classes $classes -LibDir $libDir),
    "eu.darkbot.bridge.KekkaPlayerLauncher",
    "@$(Convert-ToJavaPath $propsPath)"
) -join " "

$workDirSlash = Convert-ToJavaPath $WorkingDir
@(
    "log `"=== KekkaPlayer debug $(Get-Date -Format 'yyyy-MM-dd HH:mm') ===`""
    "log `"init args: $initArgs`""
    "init `"$java`",`"$initArgs`",`"$workDirSlash`""
    "log `"Clearing entry/TLS breakpoints, running to createWindow crash...`""
    "bca"
    "erun"
    "log `"=== CRASH STOP - see scripts/x64dbg-kekka-checklist.txt ===`""
    "log `"RIP={rip} RAX={rax} RSI={rsi} RCX={rcx}`""
    "log `"Switch thread to API thread, then: k (stack), mod (KekkaPlayer.dll)`""
    "k"
    "log `"=== END ===`""
) | Set-Content -Path $dynamicScript -Encoding ASCII

function Quote-CmdArg([string]$Value) {
    '"' + ($Value -replace '"', '\"') + '"'
}

$argumentLine = "-cf " + (Quote-CmdArg $dynamicScript)

$exe = if ($UseHeadless -and (Test-Path $headlessExe)) { $headlessExe } else { $x64dbg }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.Arguments = $argumentLine
$psi.UseShellExecute = $false

Write-Host "x64dbg: $exe"
Write-Host "Batch: $batFile"
Write-Host "Arg file: $argFile"
Write-Host "WorkingDir: $WorkingDir"
Write-Host ""

if ($UseHeadless) {
    $p = [System.Diagnostics.Process]::Start($psi)
    $p.WaitForExit()
    Write-Host "Exit code: $($p.ExitCode)"
} else {
    Write-Host "Opening x64dbg - F9 if paused"
    [System.Diagnostics.Process]::Start($psi) | Out-Null
}
