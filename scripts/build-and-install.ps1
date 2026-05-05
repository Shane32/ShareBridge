<#
.SYNOPSIS
    Builds, signs, and installs the ShareBridge MSIX package for the current user.
    No administrator permissions required.

.DESCRIPTION
    1. Creates (or reuses) a local self-signed code-signing certificate.
    2. Trusts the certificate in the current user's Trusted People store.
    3. Builds the MSIX package in Release|x64 configuration.
    4. Signs the package with the local certificate.
    5. Installs the package for the current user (per-user, no elevation).

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release).

.PARAMETER Platform
    Target platform: x64, x86, or ARM64 (default: x64).

.EXAMPLE
    .\build-and-install.ps1
    .\build-and-install.ps1 -Configuration Debug -Platform x64
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('x64', 'x86', 'ARM64')]
    [string]$Platform = 'x64'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir  = $PSScriptRoot
$RepoRoot   = Split-Path $ScriptDir -Parent
$AppProject = Join-Path $RepoRoot 'ShareBridge.App' 'ShareBridge.App.csproj'
$CertSubject = 'CN=ShareBridgeLocal'
$CertFriendlyName = 'ShareBridge Local Signing Certificate'
$CerFile = Join-Path $ScriptDir 'ShareBridgeLocal.cer'

Write-Host '=== ShareBridge build and install ===' -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# Step 1 – Certificate
# ---------------------------------------------------------------------------
Write-Host "`n[1/5] Setting up signing certificate..." -ForegroundColor Yellow

$existingCert = Get-ChildItem -Path 'Cert:\CurrentUser\My' |
    Where-Object { $_.Subject -eq $CertSubject } |
    Select-Object -First 1

if ($existingCert) {
    Write-Host "  Reusing existing certificate (Thumbprint: $($existingCert.Thumbprint))"
    $cert = $existingCert
} else {
    Write-Host '  Creating new self-signed certificate...'
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $CertSubject `
        -KeyUsage DigitalSignature `
        -FriendlyName $CertFriendlyName `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')
    Write-Host "  Certificate created (Thumbprint: $($cert.Thumbprint))"
}

# ---------------------------------------------------------------------------
# Step 2 – Trust the certificate
# ---------------------------------------------------------------------------
Write-Host "`n[2/5] Trusting certificate in CurrentUser\TrustedPeople..." -ForegroundColor Yellow

Export-Certificate -Cert $cert -FilePath $CerFile -Force | Out-Null

$alreadyTrusted = Get-ChildItem -Path 'Cert:\CurrentUser\TrustedPeople' |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint }

if ($alreadyTrusted) {
    Write-Host '  Certificate already trusted.'
} else {
    Import-Certificate -FilePath $CerFile -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' | Out-Null
    Write-Host '  Certificate imported to TrustedPeople.'
}

# ---------------------------------------------------------------------------
# Step 3 – Build MSIX
# ---------------------------------------------------------------------------
Write-Host "`n[3/5] Building MSIX ($Configuration|$Platform)..." -ForegroundColor Yellow

$OutputDir = Join-Path $RepoRoot 'AppPackages'

& dotnet build $AppProject `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:AppxPackageDir=$OutputDir `
    -p:AppxBundle=Never `
    -p:UapAppxPackageBuildMode=SideloadOnly `
    -p:GenerateAppxPackageOnBuild=true

if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

# ---------------------------------------------------------------------------
# Step 4 – Sign the MSIX
# ---------------------------------------------------------------------------
Write-Host "`n[4/5] Signing the MSIX package..." -ForegroundColor Yellow

$msixFiles = Get-ChildItem -Path $OutputDir -Filter '*.msix' -Recurse |
    Sort-Object LastWriteTime -Descending

if (-not $msixFiles) { throw "No .msix file found in $OutputDir" }

$msixPath = $msixFiles[0].FullName
Write-Host "  Signing: $msixPath"

# signtool.exe is part of Windows SDK / Build Tools
$signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
if (-not $signtool) {
    # Try common Windows SDK locations
    $candidates = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
    )
    $signtool = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $signtool) { throw "signtool.exe not found. Install Windows SDK or Build Tools." }
}

& $signtool sign `
    /fd SHA256 `
    /sha1 $cert.Thumbprint `
    /tr http://timestamp.digicert.com `
    /td SHA256 `
    $msixPath

if ($LASTEXITCODE -ne 0) { throw "signtool failed with exit code $LASTEXITCODE" }
Write-Host '  Package signed successfully.'

# ---------------------------------------------------------------------------
# Step 5 – Install for current user
# ---------------------------------------------------------------------------
Write-Host "`n[5/5] Installing MSIX for current user..." -ForegroundColor Yellow

Add-AppxPackage -Path $msixPath -ForceApplicationShutdown

Write-Host "`n=== Installation complete ===" -ForegroundColor Green
Write-Host "Share Bridge is now installed. Open the Windows Share sheet and select"
Write-Host "'Share to Outlook Classic' to use it." -ForegroundColor Cyan
