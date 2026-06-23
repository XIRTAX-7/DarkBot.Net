# Сравнение ручных кликов: главная карта vs миникарта
param(
    [int]$Seconds = 120,
    [double]$Warmup = 2
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
    Write-Host "Запустите DarkOrbit.exe и откройте карту." -ForegroundColor Yellow
    exit 1
}

Write-Host "Кликайте по карте и миникарте. Наблюдение $Seconds сек." -ForegroundColor Cyan
& $py (Join-Path $root 'unity_click_compare_test.py') -Seconds $Seconds -Warmup $Warmup
