param(
    [string]$JarPath = "$PSScriptRoot\verifier.jar",
    [int]$Port = 8091
)

if (-not (Test-Path $JarPath)) {
    Write-Error "verifier.jar not found at $JarPath"
    exit 1
}

Write-Host "Starting verifier sidecar from $JarPath"
java -jar $JarPath
