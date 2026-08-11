

$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
Set-Location $Root

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " TreasureShell Fresh-PC Bootstrap" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

function Refresh-Path {
    $env:Path =
        [Environment]::GetEnvironmentVariable("Path","Machine") + ";" +
        [Environment]::GetEnvironmentVariable("Path","User")
}


# ------------------------------------------------------------
# WINGET
# ------------------------------------------------------------

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw "winget is required. Install/update Microsoft App Installer first."
}

Write-Host "[OK] winget" -ForegroundColor Green


# ------------------------------------------------------------
# GIT
# ------------------------------------------------------------

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {

    Write-Host "[INSTALL] Git" -ForegroundColor Yellow

    winget install --id Git.Git -e `
        --accept-source-agreements `
        --accept-package-agreements

    if ($LASTEXITCODE -ne 0) {
        throw "Git installation failed."
    }

    Refresh-Path
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git remains unavailable after installation."
}

Write-Host "[OK] Git" -ForegroundColor Green


# ------------------------------------------------------------
# .NET 10 SDK
# ------------------------------------------------------------

function Test-DotNet10 {

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        return $false
    }

    $SDKs = @(dotnet --list-sdks 2>$null)

    return [bool]($SDKs | Select-String '^10\.')
}

if (-not (Test-DotNet10)) {

    Write-Host "[INSTALL] .NET 10 SDK" -ForegroundColor Yellow

    winget install --id Microsoft.DotNet.SDK.10 -e `
        --accept-source-agreements `
        --accept-package-agreements

    if ($LASTEXITCODE -ne 0) {
        throw ".NET 10 SDK installation failed."
    }

    Refresh-Path
}

if (-not (Test-DotNet10)) {
    throw ".NET 10 SDK remains unavailable after installation."
}

Write-Host "[OK] .NET 10 SDK" -ForegroundColor Green

dotnet --list-sdks |
    Where-Object { $_ -match '^10\.' } |
    ForEach-Object {
        Write-Host "     $_"
    }


# ------------------------------------------------------------
# NUGET.ORG
# ------------------------------------------------------------

Write-Host "[CHECK] nuget.org" -ForegroundColor Cyan

$PreviousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"

$NuGetSources = @()

try {
    $NuGetSources = @(dotnet nuget list source 2>&1)
}
catch {
    $NuGetSources = @()
}

$ErrorActionPreference = $PreviousErrorActionPreference

$NuGetText = $NuGetSources -join "`n"

if ($NuGetText -notmatch 'nuget\.org') {

    Write-Host "[CONFIGURE] nuget.org" -ForegroundColor Yellow

    dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to add nuget.org."
    }
}

$PreviousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"

$VerifiedNuGetSources = @(dotnet nuget list source 2>&1)

$ErrorActionPreference = $PreviousErrorActionPreference

$VerifiedNuGetText = $VerifiedNuGetSources -join "`n"

if ($VerifiedNuGetText -notmatch 'nuget\.org') {
    throw "nuget.org verification failed."
}

if ($VerifiedNuGetText -match '(?s)nuget\.org.*?\[Disabled\]') {

    Write-Host "[ENABLE] nuget.org" -ForegroundColor Yellow

    dotnet nuget enable source nuget.org

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to enable nuget.org."
    }
}

Write-Host "[OK] nuget.org" -ForegroundColor Green
# ------------------------------------------------------------
# INNO SETUP 7
# ------------------------------------------------------------

function Find-ISCC {

    $Command = Get-Command ISCC.exe -ErrorAction SilentlyContinue

    if ($Command) {
        return $Command.Source
    }

    $Candidates = @(
        "$env:ProgramFiles\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe"
    )

    return $Candidates |
        Where-Object {
            $_ -and (Test-Path $_)
        } |
        Select-Object -First 1
}

$ISCC = Find-ISCC

if (-not $ISCC) {

    Write-Host "[INSTALL] Inno Setup 7" -ForegroundColor Yellow

    winget install --id JRSoftware.InnoSetup.7 -e `
        --accept-source-agreements `
        --accept-package-agreements

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup 7 installation failed."
    }

    Refresh-Path
    $ISCC = Find-ISCC
}

if (-not $ISCC) {
    throw "Inno Setup installed, but ISCC.exe could not be located."
}

Write-Host "[OK] Inno Setup 7" -ForegroundColor Green
Write-Host "     $ISCC"


# ------------------------------------------------------------
# FINAL HUMAN CHECKPOINT
# ------------------------------------------------------------

Write-Host ""
Write-Host "==============================================" -ForegroundColor Magenta
Write-Host " One Final Checkpoint" -ForegroundColor Magenta
Write-Host "==============================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Everything TreasureShell needs is ready." -ForegroundColor Green
Write-Host ""
Write-Host "Press Enter to build the TreasureShell" -ForegroundColor Cyan
Write-Host "Windows installer. 🌀" -ForegroundColor Magenta
Write-Host ""

Read-Host | Out-Null

# ------------------------------------------------------------
# BUILD TREASURESHELL
# ------------------------------------------------------------

$Setup = Join-Path $Root "Setup-TreasureShell.ps1"

if (-not (Test-Path $Setup)) {
    throw "Setup-TreasureShell.ps1 is missing."
}

Write-Host ""
Write-Host "[BUILD] TreasureShell" -ForegroundColor Cyan

& powershell.exe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $Setup

if ($LASTEXITCODE -ne 0) {
    throw "TreasureShell build failed."
}


# ------------------------------------------------------------
# VERIFY PUBLISHED APPLICATION
# ------------------------------------------------------------

$PublishedExe = Join-Path $Root `
    "bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe"

if (-not (Test-Path $PublishedExe)) {
    throw "Published TreasureShell.exe was not found."
}

Write-Host "[OK] Published TreasureShell.exe" -ForegroundColor Green


# ------------------------------------------------------------
# BUILD WINDOWS INSTALLER
# ------------------------------------------------------------

$ISS = Join-Path $Root "Installer\TreasureShell.iss"

if (-not (Test-Path $ISS)) {
    throw "Installer\TreasureShell.iss is missing."
}

Write-Host ""
Write-Host "[PACKAGE] TreasureShell Windows Installer" -ForegroundColor Cyan

& $ISCC $ISS

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed."
}


# ------------------------------------------------------------
# VERIFY WINDOWS INSTALLER
# ------------------------------------------------------------

$Output = Join-Path $Root "Installer\Output"

$Installer = Get-ChildItem `
    -Path $Output `
    -File `
    -Filter "*.exe" `
    -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $Installer) {
    throw "Installer compilation completed but no EXE exists in Installer\Output."
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " TreasureShell ready" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Application:" -ForegroundColor Cyan
Write-Host "  $PublishedExe"
Write-Host ""
Write-Host "Installer:" -ForegroundColor Cyan
Write-Host "  $($Installer.FullName)"
Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host " TreasureShell Installer Ready" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host ""
Write-Host "The spiral is complete. 🌀" -ForegroundColor Magenta
Write-Host ""
Write-Host "Press Enter to launch the TreasureShell installer." -ForegroundColor Cyan
Write-Host ""

Read-Host | Out-Null

Start-Process $Installer.FullName

Write-Host ""



