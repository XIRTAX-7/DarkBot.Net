# Lightweight anti-debug probe (20s default)
param(
    [int]$Seconds = 20
)

$ErrorActionPreference = 'Stop'
$frida = "$env:LOCALAPPDATA\Python\pythoncore-3.14-64\Scripts\frida.exe"
if (-not (Test-Path $frida)) {
    $frida = (Get-Command frida -ErrorAction SilentlyContinue)?.Source
}
if (-not $frida) {
    Write-Host "Install: py -m pip install frida-tools" -ForegroundColor Red
    exit 1
}

$probe = Join-Path $PSScriptRoot 'unity_antidebug_probe.js'
if (-not (Get-Process DarkOrbit -ErrorAction SilentlyContinue)) {
    Write-Host "Start DarkOrbit.exe first." -ForegroundColor Yellow
    exit 1
}

Write-Host "Attaching anti-debug probe for ${Seconds}s..."
& $frida -n DarkOrbit.exe -l $probe -q --runtime=v8 -e "setTimeout(function(){}, $Seconds * 1000)"
