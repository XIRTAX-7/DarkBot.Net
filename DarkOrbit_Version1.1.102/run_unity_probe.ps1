# Attach unity_probe.js to running DarkOrbit.exe (Unity IL2CPP v1.1.102)
$ErrorActionPreference = 'Stop'

$fridaCandidates = @(
    (Get-Command frida -ErrorAction SilentlyContinue)?.Source,
    "$env:LOCALAPPDATA\Python\pythoncore-3.14-64\Scripts\frida.exe",
    "$env:LOCALAPPDATA\Python\pythoncore-3.13-64\Scripts\frida.exe",
    "$env:LOCALAPPDATA\Programs\Python\Python313\Scripts\frida.exe"
) | Where-Object { $_ -and (Test-Path $_) }

$frida = $fridaCandidates | Select-Object -First 1
if (-not $frida) {
    Write-Host "frida CLI not found. Install: py -m pip install frida-tools" -ForegroundColor Red
    exit 1
}

$probe = Join-Path $PSScriptRoot 'unity_probe.js'
$gameExe = Join-Path $PSScriptRoot 'DarkOrbit_Version1.1.102\DarkOrbit.exe'

Write-Host "Frida: $frida"
Write-Host "Probe: $probe"
Write-Host ""

$proc = Get-Process -Name DarkOrbit -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "DarkOrbit.exe not running — starting..."
    Start-Process -FilePath $gameExe -WorkingDirectory (Split-Path $gameExe)
    Write-Host "Wait for map load, then re-run this script."
    exit 0
}

Write-Host "DarkOrbit pid=$($proc.Id)"
& (Split-Path $frida -Parent)\frida-ps.exe | Select-String -Pattern 'dark' -CaseSensitive:$false
Write-Host ""
Write-Host "Attaching probe. Move your ship on the map — watch for HeroMove: x= y="
Write-Host "Ctrl+C to detach."
Write-Host ""

& $frida -n DarkOrbit.exe -l $probe
