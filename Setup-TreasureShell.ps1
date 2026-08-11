# TREASURESHELL_PORTABLE_PREFLIGHT
# Fresh-PC validation discovered during cross-machine deployment testing.

$TreasureShellSdk10 = $false

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $TreasureShellInstalledSdks = @(dotnet --list-sdks 2>$null)

    if ($TreasureShellInstalledSdks | Select-String '^10\.') {
        $TreasureShellSdk10 = $true
    }
}

if (-not $TreasureShellSdk10) {
    throw ".NET 10 SDK is required. Run Bootstrap-TreasureShell.ps1 first."
}

$TreasureShellNuGetSources = (dotnet nuget list source 2>$null) -join "`n"

if ($TreasureShellNuGetSources -notmatch 'nuget\.org') {
    throw "nuget.org is not configured. Run Bootstrap-TreasureShell.ps1 first."
}

# END TREASURESHELL_PORTABLE_PREFLIGHT

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "TreasureShell Development Setup" -ForegroundColor Yellow
Write-Host "===============================" -ForegroundColor Yellow
Write-Host ""

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Git..." -ForegroundColor Cyan
    winget install --id Git.Git -e --accept-source-agreements --accept-package-agreements
}
else {
    Write-Host "Git found." -ForegroundColor Green
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Installing .NET 10 SDK..." -ForegroundColor Cyan
    winget install --id Microsoft.DotNet.SDK.10 -e --accept-source-agreements --accept-package-agreements
}
else {
    Write-Host ".NET SDK found." -ForegroundColor Green
    dotnet --version
}

Write-Host ""
Write-Host "Restoring..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

Write-Host ""
Write-Host "Building..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

Write-Host ""
Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host ""
Write-Host "TreasureShell ready." -ForegroundColor Green
Write-Host "$Root\bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe"

