param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$BuildIfMissing = $true
)

$ErrorActionPreference = "Stop"

$exePath = Join-Path $PSScriptRoot "..\bin\$Configuration\net8.0-windows\$Runtime\publish\WinBus.Utility.exe"

if (-not (Test-Path $exePath)) {
    if ($BuildIfMissing) {
        Write-Host "EXE not found. Building now..." -ForegroundColor Yellow
        & (Join-Path $PSScriptRoot "Build-Utility-Exe.ps1") -Configuration $Configuration -Runtime $Runtime
        if ($LASTEXITCODE -ne 0) {
            throw "Build-Utility-Exe.ps1 failed."
        }
    } else {
        throw "EXE not found: $exePath"
    }
}

Write-Host "Launching WinBus Utility..." -ForegroundColor Cyan
& $exePath
