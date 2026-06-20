# Debug KekkaPlayer outside .NET — run from DarkBot.Net UI output directory (must contain lib/).
param(
    [string]$WorkingDir = "$PSScriptRoot\..\src\DarkBot.Net.Ui\bin\Debug\net10.0",
    [string]$PropertiesFile = "",
    [string]$FlashOcx = "$env:APPDATA\DarkBot\lib\DarkFlash.ocx",
    [string]$Url = "https://gbl4.darkorbit.com/",
    [string]$Sid = "",
    [string]$Preloader = "",
    [string]$Vars = "",
    [int]$Width = 1280,
    [int]$Height = 720,
    [int]$ProxyPort = 0
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "kekka-launcher-common.ps1")

$WorkingDir = (Resolve-Path $WorkingDir).Path
$javaHome = $env:JAVA_HOME
if (-not $javaHome) {
    $javaHome = "C:\Program Files\Eclipse Adoptium\jdk-11.0.31.11-hotspot"
}
$java = (Resolve-Path (Join-Path $javaHome "bin\java.exe")).Path
$bridgeRoot = (Resolve-Path "$PSScriptRoot\..\src\DarkBot.Net.Agent.Bridge").Path
$classes = Join-Path $bridgeRoot "build\classes"
$javac = Join-Path $javaHome "bin\javac.exe"

if (-not (Test-Path (Join-Path $WorkingDir "lib\KekkaPlayer.dll"))) {
    throw "lib\KekkaPlayer.dll not found under $WorkingDir"
}

Register-FlashOcx

Write-Host "Compiling bridge classes..."
New-Item -ItemType Directory -Force -Path $classes | Out-Null
$darkBotJar = Join-Path $WorkingDir "lib\DarkBot.jar"
if (-not (Test-Path $darkBotJar)) { $darkBotJar = "C:\DarkBot\DarkBot.jar" }
& $javac -encoding UTF-8 -classpath $darkBotJar -d $classes (Get-ChildItem (Join-Path $bridgeRoot "java") -Recurse -Filter "*.java" | ForEach-Object FullName)

$libDir = Join-Path $WorkingDir "lib"
$kekkaDll = Join-Path $libDir "KekkaPlayer.dll"
$argFile = Join-Path $WorkingDir "kekka-launcher.jvmargs"
$logOut = Join-Path $WorkingDir "kekka-launcher-out.log"
$logErr = Join-Path $WorkingDir "kekka-launcher-err.log"

$propsPath = $null
if ($PropertiesFile) {
    $propsPath = (Resolve-Path $PropertiesFile).Path
} elseif (Test-Path (Join-Path $WorkingDir "launch.properties")) {
    $propsPath = Join-Path $WorkingDir "launch.properties"
}

if ($propsPath) {
    Write-Host "Using properties: $propsPath"
    Write-KekkaJvmArgFile -Path $argFile -WorkingDir $WorkingDir -LibDir $libDir -KekkaDll $kekkaDll -Classes $classes -PropertiesFile $propsPath
} else {
    if (-not $Sid -or -not $Preloader -or -not $Vars) {
        Write-Host "Provide -Sid, -Preloader, -Vars or use launch.properties / -PropertiesFile"
        exit 1
    }
    Write-KekkaJvmArgFile -Path $argFile -WorkingDir $WorkingDir -LibDir $libDir -KekkaDll $kekkaDll -Classes $classes `
        -FlashOcx $FlashOcx -Url $Url -Sid $Sid -Preloader $Preloader -Vars $Vars -Width $Width -Height $Height -ProxyPort $ProxyPort
}

Write-Host "Running KekkaPlayerLauncher from $WorkingDir"
Write-Host "Arg file: $argFile"
Write-Host "stdout -> $logOut"

Push-Location $WorkingDir
try {
    $p = Start-Process -FilePath $java -ArgumentList "@$argFile" -PassThru -Wait -WorkingDirectory $WorkingDir `
        -RedirectStandardOutput $logOut -RedirectStandardError $logErr
    Write-Host "Exit code: $($p.ExitCode)"
    Write-Host "--- stdout ---"
    Get-Content $logOut -ErrorAction SilentlyContinue
    Write-Host "--- stderr ---"
    Get-Content $logErr -ErrorAction SilentlyContinue
    $hsErr = Get-ChildItem (Join-Path $WorkingDir "hs_err_pid*.log"), "$env:TEMP\hs_err_pid*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($hsErr) {
        Write-Host "Latest JVM crash log: $($hsErr.FullName)"
    }
} finally {
    Pop-Location
}
