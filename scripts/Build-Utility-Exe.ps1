param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true,
    [switch]$SingleFile = $true
)

$ErrorActionPreference = "Stop"

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }
$singleFileValue = if ($SingleFile) { "true" } else { "false" }

Write-Host "Publishing WinBus utility EXE..." -ForegroundColor Cyan
Invoke-DotNet @(
    "publish",
    ".\WinBus.Utility.csproj",
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", $selfContainedValue,
    "/p:PublishSingleFile=$singleFileValue"
)

$publishExe = Join-Path $PSScriptRoot "..\bin\$Configuration\net8.0-windows\$Runtime\publish\WinBus.Utility.exe"
if (-not (Test-Path $publishExe)) {
    throw "Published EXE not found: $publishExe"
}

Write-Host "EXE generated successfully." -ForegroundColor Green
Write-Host "EXE: $publishExe"
