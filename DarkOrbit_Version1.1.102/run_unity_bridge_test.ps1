# Smoke test for unity_bridge_agent.js
param(
    [int]$Seconds = 60
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$py = (Get-Command py -ErrorAction SilentlyContinue)?.Source
if (-not $py) { $py = (Get-Command python -ErrorAction SilentlyContinue)?.Source }
if (-not $py) {
    Write-Host "Python not found." -ForegroundColor Red
    exit 1
}

if (-not (Get-Process DarkOrbit -ErrorAction SilentlyContinue)) {
    Write-Host "Start DarkOrbit.exe and open the map first." -ForegroundColor Yellow
    exit 1
}

& $py (Join-Path $root 'unity_bridge_test.py') -Seconds $Seconds
