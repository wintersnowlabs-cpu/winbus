param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$CreateSelfSignedCert,
    [string]$CertThumbprint = ""
)

$ErrorActionPreference = "Stop"

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

Write-Host "Publishing WinBus utility..." -ForegroundColor Cyan
Invoke-DotNet @(
    "publish",
    ".\WinBus.Utility.csproj",
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "/p:PublishSingleFile=true"
)

Write-Host "Building MSI installer..." -ForegroundColor Cyan
Invoke-DotNet @(
    "build",
    ".\installer\WinBus.Installer.wixproj",
    "-c", $Configuration
)

$publishExe = Join-Path $PSScriptRoot "..\bin\$Configuration\net8.0-windows\$Runtime\publish\WinBus.Utility.exe"
$msiPath = Join-Path $PSScriptRoot "..\installer\bin\x64\$Configuration\WinBus.Utility.Installer.msi"

if (-not (Test-Path $publishExe)) {
    throw "Published EXE not found: $publishExe"
}

if (-not (Test-Path $msiPath)) {
    throw "MSI not found: $msiPath"
}

$certificate = $null

if ($CreateSelfSignedCert -and [string]::IsNullOrWhiteSpace($CertThumbprint)) {
    Write-Host "Creating self-signed code-signing certificate..." -ForegroundColor Yellow
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=WinBus Utility Dev Signer" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(2)

    $CertThumbprint = $certificate.Thumbprint
    Write-Host "Created certificate thumbprint: $CertThumbprint" -ForegroundColor Yellow
}

if (-not [string]::IsNullOrWhiteSpace($CertThumbprint)) {
    if ($null -eq $certificate) {
        $certificate = Get-ChildItem "Cert:\CurrentUser\My" |
            Where-Object { $_.Thumbprint -eq $CertThumbprint } |
            Select-Object -First 1
    }

    if ($null -eq $certificate) {
        throw "Certificate with thumbprint '$CertThumbprint' not found in Cert:\CurrentUser\My"
    }

    Write-Host "Signing EXE..." -ForegroundColor Cyan
    $exeSign = Set-AuthenticodeSignature -FilePath $publishExe -Certificate $certificate -TimestampServer "http://timestamp.digicert.com"
    if ($exeSign.Status -ne "Valid") {
        throw "EXE signing failed: $($exeSign.StatusMessage)"
    }

    Write-Host "Signing MSI..." -ForegroundColor Cyan
    $msiSign = Set-AuthenticodeSignature -FilePath $msiPath -Certificate $certificate -TimestampServer "http://timestamp.digicert.com"
    if ($msiSign.Status -ne "Valid") {
        throw "MSI signing failed: $($msiSign.StatusMessage)"
    }

    Write-Host "Signing complete." -ForegroundColor Green
} else {
    Write-Host "No certificate thumbprint supplied. Installer generated but not signed." -ForegroundColor Yellow
}

Write-Host "Completed." -ForegroundColor Green
Write-Host "EXE: $publishExe"
Write-Host "MSI: $msiPath"
