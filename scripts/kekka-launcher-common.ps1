# Shared helpers for KekkaPlayerLauncher scripts.
function Register-FlashOcx {
    param([string]$FlashPath = "$env:APPDATA\DarkBot\lib\DarkFlash.ocx")

    if (-not (Test-Path $FlashPath)) {
        Write-Warning "DarkFlash.ocx not found at $FlashPath"
        return
    }

    $p = Start-Process -FilePath "regsvr32" -ArgumentList "/s `"$FlashPath`"" -PassThru -Wait -NoNewWindow
    if ($p.ExitCode -eq 0) {
        Write-Host "Registered DarkFlash.ocx via regsvr32"
    } else {
        Write-Warning "regsvr32 exit code $($p.ExitCode) for $FlashPath"
    }
}

function Convert-ToJavaPath([string]$Path) {
    if (-not $Path) { return $Path }
    return ($Path -replace '\\', '/')
}

function Resolve-DarkBotJar([string]$LibDir) {
    $candidates = @(
        (Join-Path $LibDir "DarkBot.jar")
        "C:\DarkBot\DarkBot.jar"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

function Build-KekkaClasspath([string]$Classes, [string]$LibDir) {
    $parts = @()
    $jar = Resolve-DarkBotJar -LibDir $LibDir
    if ($jar) {
        if (-not (Test-Path (Join-Path $LibDir "DarkBot.jar")) -and $jar -ne (Join-Path $LibDir "DarkBot.jar")) {
            Copy-Item -Force $jar (Join-Path $LibDir "DarkBot.jar")
            Write-Host "Copied DarkBot.jar to $LibDir"
        }
        $parts += $jar
    } else {
        Write-Warning "DarkBot.jar not found — AuthAPI.INSTANCE will fail (need lib/DarkBot.jar or C:\DarkBot\DarkBot.jar)"
    }
    $parts += $Classes
    return ($parts | ForEach-Object { Convert-ToJavaPath $_ }) -join ';'
}

function Write-KekkaJvmArgFile {
    param(
        [string]$Path,
        [string]$WorkingDir,
        [string]$LibDir,
        [string]$KekkaDll,
        [string]$Classes,
        [string]$PropertiesFile = "",
        [string]$FlashOcx = "",
        [string]$Url = "https://gbl4.darkorbit.com/",
        [string]$Sid = "",
        [string]$Preloader = "",
        [string]$Vars = "",
        [int]$Width = 1280,
        [int]$Height = 720,
        [int]$ProxyPort = 0
    )

    $lines = @(
        "-Duser.dir=$(Convert-ToJavaPath $WorkingDir)",
        "-Djava.library.path=$(Convert-ToJavaPath $LibDir)",
        "-Ddarkbot.kekka.library=$(Convert-ToJavaPath $KekkaDll)",
        "-cp",
        (Build-KekkaClasspath -Classes $Classes -LibDir $LibDir),
        "eu.darkbot.bridge.KekkaPlayerLauncher"
    )

    if ($PropertiesFile) {
        $lines += "@$(Convert-ToJavaPath $PropertiesFile)"
    } else {
        $lines += @(
            (Convert-ToJavaPath $FlashOcx),
            $Url,
            $Sid,
            $Preloader,
            $Vars,
            "$Width",
            "$Height",
            "$ProxyPort"
        )
    }

    $lines | Set-Content -Path $Path -Encoding ASCII
    return $Path
}

function Write-KekkaDebugBatch {
    param(
        [string]$Path,
        [string]$WorkingDir,
        [string]$JavaExe,
        [string]$ArgFileName = "kekka-launcher.jvmargs"
    )

    $content = @"
@echo off
cd /d "$WorkingDir"
"$JavaExe" @$ArgFileName
"@
    $content | Set-Content -Path $Path -Encoding ASCII
    return $Path
}
